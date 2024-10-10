using CoreBotTestDD.Models;
using CoreBotTestDD.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Azure;
using CoreBotTestDD.Bots;
using Microsoft.Bot.Builder.Dialogs.Prompts;
using DonBot.Dialogs;
using DonBot.Utilities;
using DonBot.Models;

namespace CoreBotTestDD.Dialogs
{
    public class AgendarDialog : ComponentDialog
    {
        private readonly UserState _userState;
        private readonly ApiCalls _apiCalls;
        private readonly CLUService _cluService;
        private readonly DataValidation _dataValidation;
        private readonly ConversationState _conversationState;
        private readonly RegisterDialog _registerDialog;
        private readonly ScheduleAvalaibilityByMonth _scheduleAvalaibilityByMonth;
        private readonly ListaEsperaDialog _listaEsperaDialog;
        private readonly AgendarByDoctorDialog _agendarByDoctorDialog;
        private readonly ILogger _logger;
        private readonly IStatePropertyAccessor<DialogState> _dialogStateAccessor;
        private readonly IStatePropertyAccessor<UserProfileModel> _userStateAccessor;
        private readonly string UrlAseguradoraId = "https://api-insurers-test-001.azurewebsites.net/Insurance?insuranceID=";
        private readonly string UrlAseguradoraPlanId = "https://api-plans-prod-001.azurewebsites.net/InsurancePlan?InsurancePlanID=1&includeQuotes=true";

        public AgendarDialog(ILogger<MainDialog> logger, CLUService cluService, UserState userState, ConversationState conversationState, ApiCalls apiCalls, CustomChoicePrompt customChoicePrompt, RegisterDialog registerDialog, AgendarByDoctorDialog agendarByDoctorDialog, ListaEsperaDialog listaEsperaDialog, ScheduleAvalaibilityByMonth scheduleAvalaibilityByMonth)
           : base(nameof(AgendarDialog))
        {
            _userState = userState ?? throw new ArgumentNullException(nameof(userState));
            _conversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));
            _logger = logger;
            _apiCalls = apiCalls;
            _cluService = cluService;
            _registerDialog = registerDialog;
            _listaEsperaDialog = listaEsperaDialog;
            _dialogStateAccessor = _userState.CreateProperty<DialogState>("DialogState");
            _userStateAccessor = _userState.CreateProperty<UserProfileModel>("UserProfile");
            _agendarByDoctorDialog = agendarByDoctorDialog;
            _scheduleAvalaibilityByMonth = scheduleAvalaibilityByMonth;

            AddDialog(registerDialog);
            AddDialog(listaEsperaDialog);
            AddDialog(agendarByDoctorDialog);
            AddDialog(scheduleAvalaibilityByMonth);
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt), ChoiceValidatorAsync));
            AddDialog(customChoicePrompt);


            var waterfallSteps = new WaterfallStep[]
            {
                IntroStepAsync,
                HandleDocumentTypeSelectionAsync,
                GetDocumentIdAsync,
                GetAseguradoraNameAsync,
                HandleInsuranceUserSelectionAsync,
                GetAseguradoraAsync,
                HandleInsuranceSelectionAsync,
                GetAseguradoraPlanAsync,
                GetTypeConsultAsync,
                GetServiceNameAsync,
                GetServiceAsync,
                HandleServiceSelectionAsync,
                GetEspecialidadAsync,
                GetScheduleAvailabilityAsync,
                GetHandleAvailabilityChoiceAsync,
                HandleScheduleAvailabilityAsync,
                GetHandleAvailabilityHourChoiceAsync,
                HandleScheduleAvailabilitySpecificAsync,
                GetConfirmationAsync,
                HandleConfirmationCitaAsync
            };

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> IntroStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var dialogState = await _dialogStateAccessor.GetAsync(stepContext.Context, () => new DialogState(), cancellationToken);
            stepContext.Values["DialogState"] = dialogState;
            var userProfile = await _userStateAccessor.GetAsync(stepContext.Context, () => new UserProfileModel(), cancellationToken);
            userProfile.SaveData = false;
            if (userProfile.DocumentType != null)
            {
                await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                return await stepContext.NextAsync();
            }
            userProfile.IsNewUser = true;
            try
            {
                List<JObject> DocumentTypes = await _apiCalls.GetDocumentTypeAsync(userProfile.CodeCompany);
                if (DocumentTypes == null || !DocumentTypes.Any())
                {
                    await stepContext.Context.SendActivityAsync("Error en la consulta.", cancellationToken: cancellationToken);
                    await ResetUserProfile(stepContext);
                    return await stepContext.EndDialogAsync();
                }
                else
                {
                    int currentPage = userProfile.CurrentPage;
                    int itemsPerPage = 6;
                    var paginatedDocumentTypes = DocumentTypes.Skip(currentPage * itemsPerPage).Take(itemsPerPage).ToList();

                    var options = paginatedDocumentTypes.Select(documentType => new
                    {
                        Id = documentType["id"].ToString(),
                        Name = documentType["name"].ToString()
                    }).ToArray();

                    var optionsList = options.ToList();
                    if (DocumentTypes.Count > (currentPage + 1) * itemsPerPage)
                    {
                        var nextPageOption = new { Id = "SiguientePágina", Name = "Siguiente Página" };
                        optionsList.Add(nextPageOption);
                    }
                    if (currentPage > 0)
                    {
                        var previousPageOption = new { Id = "PaginaAnterior", Name = "Página Anterior" };
                        optionsList.Add(previousPageOption);
                    }
                    var backOption = new { Id = "Atras", Name = "Atras" };
                    optionsList.Add(backOption);

                    var promptOptions = new PromptOptions
                    {
                        Prompt = MessageFactory.Text("Para continuar, por favor indicamos, ❔ ¿Cuál es tu tipo y documento de identidad?"),
                        Choices = ChoiceFactory.ToChoices(optionsList.Select(option => option.Name).ToList()),
                    };

                    stepContext.Values["documentOptions"] = optionsList;
                    await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                    return await stepContext.PromptAsync(nameof(CustomChoicePrompt), promptOptions, cancellationToken);
                }

            }
            catch (Exception ex)
            {
                await stepContext.Context.SendActivityAsync("Error en la consulta");
                return await stepContext.EndDialogAsync();
            }
        }

        private async Task<DialogTurnResult> HandleDocumentTypeSelectionAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userProfile = await _userStateAccessor.GetAsync(stepContext.Context, () => new UserProfileModel(), cancellationToken);
            if (userProfile.DocumentType != null)
            {
                return await stepContext.NextAsync();
            }
            var options = (IEnumerable<dynamic>)stepContext.Values["documentOptions"];
            var choice = (FoundChoice)stepContext.Result;
            var selectedOption = options.FirstOrDefault(option => option.Name == choice.Value);
            if (selectedOption != null)
            {
                if (choice.Value.Equals("Atras", StringComparison.OrdinalIgnoreCase))
                {
                    stepContext.Context.Activity.Text = null;
                    userProfile.IsNewUser = false;
                    await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                    await stepContext.Context.SendActivityAsync("¿Cuentame, en que te puedo ayudar?", cancellationToken: cancellationToken);
                    return await stepContext.EndDialogAsync();
                }else if (choice.Value.Equals("Siguiente Página", StringComparison.OrdinalIgnoreCase))
                {
                    userProfile.CurrentPage++;
                    await _userStateAccessor.SetAsync(stepContext.Context, userProfile, cancellationToken);
                    await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                    return await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), null, cancellationToken);
                }
                else if(choice.Value.Equals("Página Anterior", StringComparison.OrdinalIgnoreCase))
                {
                    userProfile.CurrentPage--;
                    await _userStateAccessor.SetAsync(stepContext.Context, userProfile, cancellationToken);
                    await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                    return await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), null, cancellationToken);
                }
                userProfile.DocumentType = selectedOption.Id;
                await _userStateAccessor.SetAsync(stepContext.Context, userProfile, cancellationToken);
                await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);

                return await stepContext.NextAsync();
            }
            else
            {
                await stepContext.Context.SendActivityAsync("Opción seleccionada no válida.", cancellationToken: cancellationToken);
                return await stepContext.EndDialogAsync();
            }
        }

        private async Task<DialogTurnResult> GetDocumentIdAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userProfile = await _userStateAccessor.GetAsync(stepContext.Context, () => new UserProfileModel(), cancellationToken);
            userProfile.SaveData = true;
            if (userProfile.DocumentId != null)
            {
                userProfile.SaveData = false;
                await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                return await stepContext.NextAsync();
            }
            await stepContext.Context.SendActivityAsync("Escribe el número de tu documento ❔ sin puntos ni comas. (Ejemplo: 1001122345) Escribe atrás si quieres cambiar tu información anterior.");
            await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
            return new DialogTurnResult(DialogTurnStatus.Waiting);
        }

        private async Task<DialogTurnResult> GetAseguradoraNameAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userProfile = await _userStateAccessor.GetAsync(stepContext.Context, () => new UserProfileModel(), cancellationToken);
            if (stepContext.Context.Activity.Text != null && stepContext.Context.Activity.Text.ToString().Equals("Atras", StringComparison.OrdinalIgnoreCase))
            {
                userProfile.DocumentType = null;
                userProfile.DocumentId = null;
                stepContext.Context.Activity.Text = null;
                await _conversationState.SaveChangesAsync(stepContext.Context);
                await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                return await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), null, cancellationToken);
            }
            else if (stepContext.Context.Activity.Text != null && stepContext.Context.Activity.Text.ToString().Equals("Cancelar", StringComparison.OrdinalIgnoreCase))
            {
                await ResetUserProfile(stepContext);
                await stepContext.Context.SendActivityAsync("¿Te podemos ayudar en algo mas?");
                return await stepContext.EndDialogAsync();
            }
            userProfile.SaveData = false;
            if (userProfile.Aseguradora != null || userProfile.Name != null)
            {
                await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                return await stepContext.NextAsync();
            }
            if (userProfile.Name == null)
            {
                var userData = await _apiCalls.GetUserByIdAsync(userProfile.DocumentType, userProfile.DocumentId, userProfile.CodeCompany);
                try
                {
                    userProfile.UserId = userData["id"]?.ToString();
                    userProfile.Name = userData["name"]?.ToString();
                    userProfile.PlanAseguradora = userData["insurancePlan"]?["id"]?.ToString();
                    userProfile.Aseguradora = await _apiCalls.GetInsurancePlanByIdAsync(userProfile.CodeCompany, userProfile.PlanAseguradora);
                    if(userProfile.IsNewUser == true)
                    {
                        await stepContext.Context.SendActivityAsync("Bienvenido " + userProfile.Name);
                        userProfile.IsNewUser = false;
                    }
                }
                catch (Exception ex)
                {
                    await stepContext.Context.SendActivityAsync("Lo siento, no te encuentras registrado.");
                    stepContext.Context.Activity.Text = null;
                    await _conversationState.SaveChangesAsync(stepContext.Context);
                    return await stepContext.BeginDialogAsync(nameof(RegisterDialog), null, cancellationToken);
                }
            }
            var choices = new List<Choice>
             {      
                 new Choice { Value = "Si" },
                 new Choice { Value = "No" },
                 new Choice { Value = "Atras" }
             };
            var promptOptions = new PromptOptions
            {
                Prompt = MessageFactory.Text("Tienes asociado el convenio " + await _apiCalls.GetVariablesNameAsync(UrlAseguradoraId, userProfile.CodeCompany, userProfile.Aseguradora) + " - " + await _apiCalls.GetVariablesNameAsync(UrlAseguradoraPlanId, userProfile.CodeCompany, userProfile.Aseguradora) + " ¿Quieres agendar tu cita con esta aseguradora? Escribe el numero de la opcion que deseas."),
                Choices = choices,
                Style = ListStyle.List
            };
            stepContext.Values["Choice"] = choices;
            await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
            return await stepContext.PromptAsync(nameof(CustomChoicePrompt), promptOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> HandleInsuranceUserSelectionAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userProfile = await _userStateAccessor.GetAsync(stepContext.Context, () => new UserProfileModel(), cancellationToken);
            var cancellationReason = stepContext.Result as dynamic;
            try
            {
                if (cancellationReason.Reason != null && cancellationReason.Reason == DialogReason.CancelCalled)
                {
                    await ResetUserProfile(stepContext, cancellationToken);
                    return await stepContext.EndDialogAsync();
                }
            }
            catch (Exception e) { };
            var choiceResult = stepContext.Result as FoundChoice;
            if (userProfile.Aseguradora != null && choiceResult == null)
            {
                return await stepContext.NextAsync();
            }
            userProfile.InsuranceInclude = false;
            var choice = (FoundChoice)stepContext.Result;
            if (choice != null)
            {
                if (choice.Value.Equals("Si", StringComparison.OrdinalIgnoreCase))
                {
                    userProfile.InsuranceInclude = true;
                    await _conversationState.SaveChangesAsync(stepContext.Context);
                    stepContext.Context.Activity.Text = null;
                    return await stepContext.NextAsync();
                }else if (choice.Value.Equals("No", StringComparison.OrdinalIgnoreCase))
                {
                    stepContext.Context.Activity.Text = null;
                    userProfile.Aseguradora = null;
                    userProfile.PlanAseguradora = null;
                    await _conversationState.SaveChangesAsync(stepContext.Context);
                    await stepContext.Context.SendActivityAsync("¿Con que entidad de salud deseas solicitar la cita? (Ejemplo: Sura) Escribe atras si quieres cambiar tu informacion anterior.");
                    return new DialogTurnResult(DialogTurnStatus.Waiting);
                }
                else if (choice.Value.Equals("Atras", StringComparison.OrdinalIgnoreCase))
                {
                    stepContext.Context.Activity.Text = null;
                    userProfile.Aseguradora = null;
                    userProfile.PlanAseguradora = null;
                    userProfile.DocumentId = null;
                    userProfile.Name = null;
                    await _conversationState.SaveChangesAsync(stepContext.Context);
                    await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                    return await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), null, cancellationToken);
                }
                else
                {
                    await stepContext.Context.SendActivityAsync("Opción seleccionada no válida.", cancellationToken: cancellationToken);
                    return await stepContext.EndDialogAsync();
                }
            }
            else
            {
                await stepContext.Context.SendActivityAsync("Opción seleccionada no válida.", cancellationToken: cancellationToken);
                return await stepContext.EndDialogAsync();
            }
        }

        private async Task<DialogTurnResult> GetAseguradoraAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var dialogState = await _dialogStateAccessor.GetAsync(stepContext.Context, () => new DialogState(), cancellationToken);
            var cancellationReason = stepContext.Result as dynamic;
            try
            {
                if (cancellationReason.Reason != null && cancellationReason.Reason == DialogReason.CancelCalled)
                {
                    await ResetUserProfile(stepContext, cancellationToken);
                    return await stepContext.EndDialogAsync();
                }
            }catch(Exception e) { };
            stepContext.Values["DialogState"] = dialogState;
            var userProfile = await _userStateAccessor.GetAsync(stepContext.Context, () => new UserProfileModel(), cancellationToken);
            if (stepContext.Context.Activity.Text != null && stepContext.Context.Activity.Text.ToString().Equals("Atras", StringComparison.OrdinalIgnoreCase))
            {
                switch (userProfile.InsuranceInclude)
                {
                    case true:
                        return await stepContext.BeginDialogAsync(nameof(RegisterDialog), null, cancellationToken);
                    case false:
                        userProfile.Name = null;
                        userProfile.Aseguradora = null;
                        stepContext.Context.Activity.Text = null;
                        break;
                }
                await _conversationState.SaveChangesAsync(stepContext.Context);
                await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                return await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), null, cancellationToken);
            }else if(stepContext.Context.Activity.Text != null && stepContext.Context.Activity.Text.ToString().Equals("Cancelar", StringComparison.OrdinalIgnoreCase))
            {
                await ResetUserProfile(stepContext);
                await stepContext.Context.SendActivityAsync("¿Te podemos ayudar en algo mas?");
                return await stepContext.EndDialogAsync();
            }
            if (userProfile.Aseguradora != null)
            {
                return await stepContext.NextAsync();
            }
            try
            {
                List<JObject> aseguradoras = await _apiCalls.GetAseguradorasAsync(stepContext.Context.Activity.Text.ToString(), userProfile.CodeCompany);
                if (aseguradoras == null || !aseguradoras.Any())
                {
                    await stepContext.Context.SendActivityAsync("No se encontraron aseguradoras disponibles.", cancellationToken: cancellationToken);
                    return await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), null, cancellationToken);
                }
                else
                {
                    var options = aseguradoras.Select(documentType => new
                    {
                        Id = documentType["id"].ToString(),
                        Name = documentType["name"].ToString()
                    }).ToArray(); var optionsList = options.ToList();
                    var newOption = new
                    {
                        Id = "Atras",
                        Name = "Atras"
                    };

                    optionsList.Add(newOption);
                    var promptOptions = new PromptOptions
                    {
                        Prompt = MessageFactory.Text("Selecciona una aseguradora:"),
                        Choices = ChoiceFactory.ToChoices(optionsList.Select(option => option.Name).ToList()),
                        Style = ListStyle.List
                    };
                    stepContext.Values["Insurances"] = optionsList;
                    await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                    return await stepContext.PromptAsync(nameof(CustomChoicePrompt), promptOptions, cancellationToken);
                }

            }
            catch (Exception ex)
            {
                await stepContext.Context.SendActivityAsync("Error en la consulta");
                return await stepContext.EndDialogAsync();
            }

        }

        private async Task<DialogTurnResult> HandleInsuranceSelectionAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userProfile = await _userStateAccessor.GetAsync(stepContext.Context, () => new UserProfileModel(), cancellationToken);
            if (userProfile.Aseguradora != null)
            {
                return await stepContext.NextAsync();
            }
            var options = (IEnumerable<dynamic>)stepContext.Values["Insurances"];
            var choice = (FoundChoice)stepContext.Result;
            var selectedOption = options.FirstOrDefault(option => option.Name == choice.Value);

            if (selectedOption != null)
            {
                if (choice.Value != null && choice.Value.Equals("Atras", StringComparison.OrdinalIgnoreCase))
                {
                    stepContext.Context.Activity.Text = null;
                    await _conversationState.SaveChangesAsync(stepContext.Context);
                    await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                    return await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), null, cancellationToken);
                }
                userProfile.Aseguradora = selectedOption.Id;
                await _userStateAccessor.SetAsync(stepContext.Context, userProfile, cancellationToken);
                await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);

                return await stepContext.NextAsync();
            }
            else
            {
                await stepContext.Context.SendActivityAsync("Opción seleccionada no válida.", cancellationToken: cancellationToken);
                return await stepContext.EndDialogAsync();
            }
        }

        private async Task<DialogTurnResult> GetAseguradoraPlanAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var dialogState = await _dialogStateAccessor.GetAsync(stepContext.Context, () => new DialogState(), cancellationToken);
            stepContext.Values["DialogState"] = dialogState;
            var userProfile = await _userStateAccessor.GetAsync(stepContext.Context, () => new UserProfileModel(), cancellationToken);
            if (userProfile.PlanAseguradora != null)
            {
                return await stepContext.NextAsync();
            }
            try
            {
                List<JObject> aseguradoras = await _apiCalls.GetInsurancePlanAsync(userProfile.Aseguradora, userProfile.CodeCompany);
                if (aseguradoras == null || !aseguradoras.Any())
                {
                    await stepContext.Context.SendActivityAsync("No se encontraron planes.", cancellationToken: cancellationToken);
                    return await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), null, cancellationToken);
                }
                else
                {
                    var options = aseguradoras.Select(documentType => new
                    {
                        Id = documentType["id"].ToString(),
                        Name = documentType["name"].ToString()
                    }).ToArray(); var optionsList = options.ToList();
                    var newOption = new
                    {
                        Id = "Atras",
                        Name = "Atras"
                    };

                    optionsList.Add(newOption);
                    var promptOptions = new PromptOptions
                    {
                        Prompt = MessageFactory.Text("Selecciona el plan de tu aseguradora:"),
                        Choices = ChoiceFactory.ToChoices(optionsList.Select(option => option.Name).ToList()),
                        Style = ListStyle.List
                    };
                    stepContext.Values["InsurancesPlan"] = optionsList;
                    await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                    return await stepContext.PromptAsync(nameof(CustomChoicePrompt), promptOptions, cancellationToken);
                }

            }
            catch (Exception ex)
            {
                await stepContext.Context.SendActivityAsync("Error en la consulta");
                return await stepContext.EndDialogAsync();
            }

        }

        private async Task<DialogTurnResult> GetTypeConsultAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var choice = (FoundChoice)stepContext.Result;
            var userProfile = await _userStateAccessor.GetAsync(stepContext.Context, () => new UserProfileModel(), cancellationToken);
            if (choice != null && choice.Value.Equals("Atras", StringComparison.OrdinalIgnoreCase))
            {
                userProfile.Aseguradora = null;
                stepContext.Context.Activity.Text = null;
                await _conversationState.SaveChangesAsync(stepContext.Context);
                await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                return await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), null, cancellationToken);
            }
            if (userProfile.TypeConsult != null)
            {
                return await stepContext.NextAsync();
            }
            if (userProfile.PlanAseguradora == null)
            {
                var options = (IEnumerable<dynamic>)stepContext.Values["InsurancesPlan"];
                var selectedOption = options.FirstOrDefault(option => option.Name == choice.Value);
                userProfile.PlanAseguradora = selectedOption.Id;
            }
            var choices = new List<Choice>
        {
            new Choice { Value = "Nombre del profesional" },
            new Choice { Value = "Servicio" },
            new Choice { Value = "Atras" },
            new Choice { Value = "Cancelar" }
        };
            var promptOptions = new PromptOptions
            {
                Prompt = MessageFactory.Text("Puedes escoger realizar el agendamiento por:"),
                Choices = choices,
                Style = ListStyle.List
            };
            stepContext.Values["Choice"] = choices;
            await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
            return await stepContext.PromptAsync(nameof(CustomChoicePrompt), promptOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> GetServiceNameAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            var userProfile = await _userStateAccessor.GetAsync(stepContext.Context, () => new UserProfileModel(), cancellationToken);
            var choice = stepContext.Result as FoundChoice;
            if (userProfile.Servicios != null)
            {
                return await stepContext.NextAsync();
            }
            if (choice == null)
            {
                choice = new FoundChoice { Value = userProfile.TypeConsult };
            }
            choice.Value ??= userProfile.TypeConsult;
            if(userProfile.ServiceName != null)
            {
                choice.Value = "Service";
            }
            if (choice != null || userProfile.TypeConsult != null)
            {
                if (choice != null && choice.Value.Equals("Atras", StringComparison.OrdinalIgnoreCase))
                {
                    if (userProfile.InsuranceInclude == true)
                    {
                        userProfile.Name = null;
                        userProfile.Aseguradora = null;
                    }
                    userProfile.PlanAseguradora = null;
                    stepContext.Context.Activity.Text = null;
                    await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                    return await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), null, cancellationToken);
                }
                else if (choice.Value.Equals("Nombre del profesional", StringComparison.OrdinalIgnoreCase))
                {
                    stepContext.Context.Activity.Text = null;
                    userProfile.TypeConsult = "Doctor";
                    await _conversationState.SaveChangesAsync(stepContext.Context);
                    return await stepContext.BeginDialogAsync(nameof(AgendarByDoctorDialog), null, cancellationToken);
                }
                else if (choice.Value.Equals("Cancelar", StringComparison.OrdinalIgnoreCase))
                {
                    await stepContext.Context.SendActivityAsync("¿Te podemos ayudar en algo mas?");
                    await ResetUserProfile(stepContext, cancellationToken);
                    return await stepContext.EndDialogAsync();
                }
                else
                {
                    if(userProfile.ServiceName != null)
                    {
                        return await stepContext.NextAsync();
                    }
                    userProfile.TypeConsult = "Service";
                    await stepContext.Context.SendActivityAsync("¿Qué servicio deseas agendar 🗓️? (Ejemplo: Cardiología) \n\n Escribe atrás si quieres cambiar tu información anterior.");
                    userProfile.SaveData = false;
                    await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                    return new DialogTurnResult(DialogTurnStatus.Waiting);
                }

            }
            else
            {
                await stepContext.Context.SendActivityAsync("Opción seleccionada no válida.", cancellationToken: cancellationToken);
                return await stepContext.EndDialogAsync();
            }
        }

        private async Task<DialogTurnResult> GetServiceAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var dialogState = await _dialogStateAccessor.GetAsync(stepContext.Context, () => new DialogState(), cancellationToken);
            stepContext.Values["DialogState"] = dialogState;
            var userProfile = await _userStateAccessor.GetAsync(stepContext.Context, () => new UserProfileModel(), cancellationToken);
            var cancellationReason = stepContext.Result as dynamic;
            try
            {
                if (cancellationReason.Reason != null && cancellationReason.Reason == DialogReason.CancelCalled)
                {
                    await ResetUserProfile(stepContext, cancellationToken);
                    return await stepContext.EndDialogAsync();
                }else if (cancellationReason.Reason != null && cancellationReason.Reason == DialogReason.ReplaceCalled)
                {
                    userProfile.TypeConsult = null;
                    stepContext.Context.Activity.Text = null;
                    await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                    return await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), null, cancellationToken);
                }
            }
            catch (Exception e) { };
            if (stepContext.Context.Activity.Text != null && stepContext.Context.Activity.Text.ToString().Equals("Atras", StringComparison.OrdinalIgnoreCase))
            {
                userProfile.TypeConsult = null;
                stepContext.Context.Activity.Text = null;
                await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                return await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), null, cancellationToken);
            }
            else if (stepContext.Context.Activity.Text != null && stepContext.Context.Activity.Text.ToString().Equals("Cancelar", StringComparison.OrdinalIgnoreCase))
            {
                await ResetUserProfile(stepContext);
                await stepContext.Context.SendActivityAsync("¿Te podemos ayudar en algo mas?");
                return await stepContext.EndDialogAsync();
            }
            if (userProfile.Servicios != null)
            {
                return await stepContext.NextAsync();
            };
            try
            {
                List<JObject> Servicios;
                ResponseCLUPrompt text;
                if (userProfile.ServiceName != null)
                {
                    text = await _cluService.AnalyzeTextEntitiesAsync(userProfile.ServiceName);
                }
                else
                {
                    text = await _cluService.AnalyzeTextEntitiesAsync(stepContext.Context.Activity.Text.ToString());
                    userProfile.ServiceName = stepContext.Context.Activity.Text.ToString();
                }
                if (text != null)
                {
                    Servicios = await _apiCalls.GetServicesAsync(text.intent, userProfile.CodeCompany);
                }
                else
                {
                    Servicios = await _apiCalls.GetServicesAsync(stepContext.Context.Activity.Text.ToString(), userProfile.CodeCompany);
                }
                if (Servicios == null || !Servicios.Any())
                {
                    await stepContext.Context.SendActivityAsync("No se encontraron servicios disponibles.", cancellationToken: cancellationToken);
                    userProfile.ServiceName = null;
                    return await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), null, cancellationToken);
                }
                else
                {
                    List<string> servicios = new List<string>();
                    foreach (var servicio in Servicios)
                    {
                        servicios.Add(servicio.Value<string>("name"));
                    }
                    int currentPage = userProfile.CurrentPage;
                    int itemsPerPage = 6;
                    var paginatedSchedule = Servicios.Skip(currentPage * itemsPerPage).Take(itemsPerPage).ToList();
                    var options = paginatedSchedule.Select(documentType => new
                    {
                        Id = documentType["id"].ToString(),
                        Name = documentType["name"].ToString()
                    }).ToArray(); 
                    var optionsList = options.ToList();
                    if (Servicios.Count > (currentPage + 1) * itemsPerPage)
                    {
                        var nextPageOption = new { Id = "SiguientePágina", Name = "Siguiente Página" };
                        optionsList.Add(nextPageOption);
                    }
                    if (currentPage > 0)
                    {
                        var previousPageOption = new { Id = "PaginaAnterior", Name = "Página Anterior" };
                        optionsList.Add(previousPageOption);
                    }
                    var backOption = new { Id = "Atras", Name = "Atras" };
                    optionsList.Add(backOption);
                    var promptOptions = new PromptOptions
                    {
                        Prompt = MessageFactory.Text("Selecciona un servicio:"),
                        Choices = ChoiceFactory.ToChoices(optionsList.Select(option => option.Name).ToList()),
                        Style = ListStyle.List
                    };
                    stepContext.Values["Services"] = optionsList;
                    await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                    return await stepContext.PromptAsync(nameof(CustomChoicePrompt), promptOptions, cancellationToken);
                }

            }
            catch (Exception ex)
            {
                await stepContext.Context.SendActivityAsync("Error en la consulta");
                return await stepContext.EndDialogAsync();
            }

        }

        private async Task<DialogTurnResult> HandleServiceSelectionAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            var choice = (FoundChoice)stepContext.Result;
            var userProfile = await _userStateAccessor.GetAsync(stepContext.Context, () => new UserProfileModel(), cancellationToken);
            if (userProfile.Servicios != null)
            {
                return await stepContext.NextAsync();
            }
            var options = (IEnumerable<dynamic>)stepContext.Values["Services"];
            var selectedOption = options.FirstOrDefault(option => option.Name == choice.Value);
            if (selectedOption != null)
            {
                if (choice != null && choice.Value.Equals("Atras", StringComparison.OrdinalIgnoreCase))
                {
                    userProfile.ServiceName = null;
                    userProfile.CurrentPage = 0;
                    stepContext.Context.Activity.Text = null;
                    await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                    return await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), null, cancellationToken);
                }
                else if (choice.Value.Equals("Siguiente Página", StringComparison.OrdinalIgnoreCase))
                {
                    userProfile.CurrentPage++;
                    await _userStateAccessor.SetAsync(stepContext.Context, userProfile, cancellationToken);
                    await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                    return await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), null, cancellationToken);
                }
                else if (choice.Value.Equals("Pagina Anterior", StringComparison.OrdinalIgnoreCase))
                {
                    userProfile.CurrentPage--;
                    await _userStateAccessor.SetAsync(stepContext.Context, userProfile, cancellationToken);
                    await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                    return await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), null, cancellationToken);
                }
                userProfile.Servicios = selectedOption.Id;
                await _userStateAccessor.SetAsync(stepContext.Context, userProfile, cancellationToken);
                await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);

                return await stepContext.NextAsync();
            }
            else
            {
                await stepContext.Context.SendActivityAsync("Opción seleccionada no válida.", cancellationToken: cancellationToken);
                return await stepContext.EndDialogAsync();
            }
        }

        private async Task<DialogTurnResult> GetEspecialidadAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var dialogState = await _dialogStateAccessor.GetAsync(stepContext.Context, () => new DialogState(), cancellationToken);
            stepContext.Values["DialogState"] = dialogState;
            var choice = (FoundChoice)stepContext.Result;
            var userProfile = await _userStateAccessor.GetAsync(stepContext.Context, () => new UserProfileModel(), cancellationToken);
            if (choice != null && choice.Value.Equals("Atras", StringComparison.OrdinalIgnoreCase))
            {
                userProfile.ServiceName = null;
                userProfile.Aseguradora = null;
                stepContext.Context.Activity.Text = null;
                await _conversationState.SaveChangesAsync(stepContext.Context);
                await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                return await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), null, cancellationToken);
            }
            if (userProfile.especialidad != null)
            {
                return await stepContext.NextAsync();
            };
            try
            {
                List<JObject> especialidades = await _apiCalls.GetEspecialidadAsync(userProfile.Servicios, userProfile.CodeCompany);
                if (especialidades == null || !especialidades.Any())
                {
                    await stepContext.Context.SendActivityAsync("No se encontraron especialidades disponibles.", cancellationToken: cancellationToken);
                    userProfile.Servicios = null;
                    return await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), null, cancellationToken);
                }
                else
                {
                    if(especialidades.Count == 1)
                    {
                        userProfile.especialidad = especialidades[0]["id"].ToString();
                        await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                        return await stepContext.NextAsync();
                    }
                    var options = especialidades.Select(documentType => new
                    {
                        Id = documentType["id"].ToString(),
                        Name = documentType["name"].ToString()
                    }).ToArray(); var optionsList = options.ToList();
                    var newOption = new
                    {
                        Id = "Atras",
                        Name = "Atras"
                    };

                    optionsList.Add(newOption);
                    var promptOptions = new PromptOptions
                    {
                        Prompt = MessageFactory.Text("Selecciona una especialidad:"),
                        Choices = ChoiceFactory.ToChoices(optionsList.Select(option => option.Name).ToList()),
                        Style = ListStyle.List
                    };
                    stepContext.Values["Especialidades"] = optionsList;
                    await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                    return await stepContext.PromptAsync(nameof(CustomChoicePrompt), promptOptions, cancellationToken);
                }

            }
            catch (Exception ex)
            {
                await stepContext.Context.SendActivityAsync("Error en la consulta");
                return await stepContext.EndDialogAsync();
            }

        }

        private async Task<DialogTurnResult> GetScheduleAvailabilityAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userProfile = await _userStateAccessor.GetAsync(stepContext.Context, () => new UserProfileModel(), cancellationToken);
            if (userProfile.AvailabilityChoiceMonth != 0)
            {
                return await stepContext.NextAsync();
            }
            if(userProfile.especialidad == null)
            {
                var optionsAnt = (IEnumerable<dynamic>)stepContext.Values["Especialidades"];
                var choice = (FoundChoice)stepContext.Result;
                if (choice != null && choice.Value.Equals("Atras", StringComparison.OrdinalIgnoreCase))
                {
                    userProfile.Servicios = null;
                    stepContext.Context.Activity.Text = null;
                    await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                    return await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), null, cancellationToken);
                }
                var selectedOption = optionsAnt.FirstOrDefault(option => option.Name == choice.Value);
                var dialogState = await _dialogStateAccessor.GetAsync(stepContext.Context, () => new DialogState(), cancellationToken);
                stepContext.Values["DialogState"] = dialogState;
                userProfile.especialidad = selectedOption.Id;
            }
            var choices = new List<Choice>
             {
                   new Choice { Value = "Ver la cita más cercana disponible." },
                    new Choice { Value = "Ver citas disponibles en la próxima semana." },
                 new Choice { Value = "Ver citas disponibles dentro del próximo mes." },
                 new Choice { Value = "Seleccionar un mes en especifico" },
                 new Choice { Value = "Atras" }
             };
            var promptOptions = new PromptOptions
            {
                Prompt = MessageFactory.Text("Para continuar con el agendamiento de tu cita, por favor selecc de las siguientes opciones: "),
                Choices = choices,
                Style = ListStyle.List
            };
            stepContext.Values["Choice"] = choices;
            await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
            return await stepContext.PromptAsync(nameof(CustomChoicePrompt), promptOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> GetHandleAvailabilityChoiceAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            var userProfile = await _userStateAccessor.GetAsync(stepContext.Context, () => new UserProfileModel(), cancellationToken);
            var cancellationReasonInit = stepContext.Result as dynamic;
            try
            {
                if (cancellationReasonInit.Reason != null && cancellationReasonInit.Reason == DialogReason.CancelCalled)
                {
                    await ResetUserProfile(stepContext, cancellationToken);
                    return await stepContext.EndDialogAsync();
                }
            }
            catch (Exception e) { };
            var choice = stepContext.Result as FoundChoice;
            if (stepContext.Values.ContainsKey("DataCita"))
            {
                return await stepContext.NextAsync();
            }
                if (choice != null && choice.Value.Equals("Atras", StringComparison.OrdinalIgnoreCase))
                {
                    List<JObject> especialidades = await _apiCalls.GetEspecialidadAsync(userProfile.Servicios, userProfile.CodeCompany);
                    if (especialidades.Count == 1)
                    {
                        userProfile.Servicios = null;
                    }
                    userProfile.especialidad = null;
                    stepContext.Context.Activity.Text = null;
                    await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                    return await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), null, cancellationToken);
                }
                
                List<JObject> ScheduleAvailability = null;
                if (choice != null)
                {
                    switch (choice.Value)
                    {
                        case "Ver citas disponibles en la próxima semana.":
                            userProfile.AvailabilityChoiceMonth = 2;
                            ScheduleAvailability = await _apiCalls.GetScheduleAvailabilityAsync(userProfile, "2");
                            break;
                        case "Ver citas disponibles dentro del próximo mes.":
                            userProfile.AvailabilityChoiceMonth = 3;
                            ScheduleAvailability = await _apiCalls.GetScheduleAvailabilityAsync(userProfile, "3");
                            break;
                        case "Ver la cita más cercana disponible.":
                            userProfile.AvailabilityChoiceMonth = 1;
                            ScheduleAvailability = await _apiCalls.GetScheduleAvailabilityAsync(userProfile, "1");
                            break;
                        case "Seleccionar un mes en especifico":
                            return await stepContext.BeginDialogAsync(nameof(ScheduleAvalaibilityByMonth), null, cancellationToken);
                    };
            }
            else
                {
                    ScheduleAvailability = await _apiCalls.GetScheduleAvailabilityAsync(userProfile, userProfile.AvailabilityChoiceMonth.ToString());
                }
                if (ScheduleAvailability == null || !ScheduleAvailability.Any())
                {
                    await stepContext.Context.SendActivityAsync("No se encontro agenda disponible. ", cancellationToken: cancellationToken);
                    stepContext.Context.Activity.Text = null;
                    await _conversationState.SaveChangesAsync(stepContext.Context);
                    return await stepContext.BeginDialogAsync(nameof(ListaEsperaDialog), null, cancellationToken);
                }
                else
                {
                    int currentPage = userProfile.CurrentPage;
                    int itemsPerPage = 6;
                    var paginatedSchedule = ScheduleAvailability.Skip(currentPage * itemsPerPage).Take(itemsPerPage).ToList();
                    var options = paginatedSchedule.Select(documentType => new
                    {
                        DateTime = documentType.GetValue("dateTime").ToString(),
                        UserId = documentType.GetValue("userID").ToString(),
                        AppointmentTypeId = documentType.GetValue("appointmentTypeID").ToString(),
                        Duration = documentType.GetValue("duration").ToString(),
                        OfficeId = documentType.GetValue("officeID").ToString(),
                        DoctorName = documentType.GetValue("doctorName").ToString()
                    }).ToArray();
                    var optionsList = options.ToList();
                    if (ScheduleAvailability.Count > (currentPage + 1) * itemsPerPage)
                    {
                        var nextPageOption = new { DateTime = "Siguiente Página", UserId = "Siguiente Página", AppointmentTypeId = "", Duration = "", OfficeId = "", DoctorName = "" };
                        optionsList.Add(nextPageOption);
                    }
                    if (currentPage > 0)
                    {
                        var previousPageOption = new { DateTime = "Pagina Anterior", UserId = "Página Anterior", AppointmentTypeId = "", Duration = "", OfficeId = "", DoctorName = "" };
                        optionsList.Add(previousPageOption);
                    }
                    var backOption = new { DateTime = "Atras", UserId = "Atras", AppointmentTypeId = "", Duration = "", OfficeId = "", DoctorName = "" };
                    optionsList.Add(backOption);

                    var promptOptions = new PromptOptions
                    {
                        Prompt = MessageFactory.Text("Selecciona una fecha para tu cita: " + Environment.NewLine),
                        Choices = ChoiceFactory.ToChoices(optionsList.Select(option => option.DateTime).ToList()),
                        Style = ListStyle.List
                    };
                    stepContext.Values["ScheduleAvailability"] = options;
                    await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                    return await stepContext.PromptAsync(nameof(CustomChoicePrompt), promptOptions, cancellationToken);
                }
        }

        private async Task<DialogTurnResult> HandleScheduleAvailabilityAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userProfile = await _userStateAccessor.GetAsync(stepContext.Context, () => new UserProfileModel(), cancellationToken);
            var cancellationReason = stepContext.Result as dynamic;
            try
            {
                if (cancellationReason.Reason != null && cancellationReason.Reason == DialogReason.CancelCalled)
                {
                    await ResetUserProfile(stepContext, cancellationToken);
                    return await stepContext.EndDialogAsync();
                }else if(cancellationReason.Reason != null && cancellationReason.Reason == DialogReason.ReplaceCalled)
                {
                    List<JObject> especialidades = await _apiCalls.GetEspecialidadAsync(userProfile.Servicios, userProfile.CodeCompany);
                    if (especialidades.Count == 1)
                    {
                        userProfile.Servicios = null;
                    }
                    userProfile.AvailabilityChoiceMonth = 0;
                    stepContext.Context.Activity.Text = null;
                    await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                    return await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), null, cancellationToken);
                }
            }
            catch (Exception e) { };
            var choice = (FoundChoice)stepContext.Result;
            if (choice != null && choice.Value.Equals("Atras", StringComparison.OrdinalIgnoreCase))
            {
                List<JObject> especialidades = await _apiCalls.GetEspecialidadAsync(userProfile.Servicios, userProfile.CodeCompany);
                if(especialidades.Count == 1)
                {
                    userProfile.Servicios = null;
                }
                userProfile.AvailabilityChoiceMonth = 0;
                stepContext.Context.Activity.Text = null;
                await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                return await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), null, cancellationToken);
            }
            else if (choice.Value.Equals("Siguiente Página", StringComparison.OrdinalIgnoreCase))
            {
                userProfile.CurrentPage++;
                await _userStateAccessor.SetAsync(stepContext.Context, userProfile, cancellationToken);
                await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                return await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), null, cancellationToken);
            }
            else if (choice.Value.Equals("Pagina Anterior", StringComparison.OrdinalIgnoreCase))
            {
                userProfile.CurrentPage--;
                await _userStateAccessor.SetAsync(stepContext.Context, userProfile, cancellationToken);
                await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                return await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), null, cancellationToken);
            }
            var options = (IEnumerable<dynamic>)stepContext.Values["ScheduleAvailability"];
            var selectedOption = options.FirstOrDefault(option => option.DateTime == choice.Value);

            if (selectedOption != null)
            {
                userProfile.CurrentPage = 0;
                stepContext.Values["DataCita"] = selectedOption;
                return await stepContext.NextAsync();
            }
            else
            {
                await stepContext.Context.SendActivityAsync("Opción seleccionada no válida.", cancellationToken: cancellationToken);
                return await stepContext.EndDialogAsync();
            }
        }

        private async Task<DialogTurnResult> GetHandleAvailabilityHourChoiceAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            var userProfile = await _userStateAccessor.GetAsync(stepContext.Context, () => new UserProfileModel(), cancellationToken);
            List<JObject> ScheduleAvailability = null;
            ScheduleAvailability = await _apiCalls.GetScheduleAvailabilitySpecificAsync(userProfile, (string)stepContext.Values["DataCita"].GetType().GetProperty("DateTime").GetValue(stepContext.Values["DataCita"]));
            if (ScheduleAvailability == null || !ScheduleAvailability.Any())
            {
                await stepContext.Context.SendActivityAsync("No se encontro agenda disponible. ", cancellationToken: cancellationToken);
                stepContext.Context.Activity.Text = null;
                await _conversationState.SaveChangesAsync(stepContext.Context);
                return await stepContext.BeginDialogAsync(nameof(ListaEsperaDialog), null, cancellationToken);
            }
            else
            {
                int currentPage = userProfile.CurrentPage;
                int itemsPerPage = 6;
                var paginatedSchedule = ScheduleAvailability.Skip(currentPage * itemsPerPage).Take(itemsPerPage).ToList();
                var options = paginatedSchedule.Select(documentType => new
                {
                    DateTime = documentType.GetValue("dateTime").ToString(),
                    UserId = documentType.GetValue("userID").ToString(),
                    AppointmentTypeId = documentType.GetValue("appointmentTypeID").ToString(),
                    Duration = documentType.GetValue("duration").ToString(),
                    OfficeId = documentType.GetValue("officeID").ToString(),
                    DoctorName = documentType.GetValue("doctorName").ToString()
                }).ToArray();
                var optionsList = options.ToList();
                if (ScheduleAvailability.Count > (currentPage + 1) * itemsPerPage)
                {
                    var nextPageOption = new { DateTime = "Siguiente Página", UserId = "Siguiente Página", AppointmentTypeId = "", Duration = "", OfficeId = "", DoctorName = "" };
                    optionsList.Add(nextPageOption);
                }
                if (currentPage > 0)
                {
                    var previousPageOption = new { DateTime = "Pagina Anterior", UserId = "Página Anterior", AppointmentTypeId = "", Duration = "", OfficeId = "", DoctorName = "" };
                    optionsList.Add(previousPageOption);
                }
                var backOption = new { DateTime = "Atras", UserId = "Atras", AppointmentTypeId = "", Duration = "", OfficeId = "", DoctorName = "" };
                optionsList.Add(backOption);

                var promptOptions = new PromptOptions
                {
                    Prompt = MessageFactory.Text("Selecciona una hora para tu cita: " + Environment.NewLine),
                    Choices = ChoiceFactory.ToChoices(optionsList.Select(option => option.DateTime).ToList()),
                    Style = ListStyle.List
                };
                stepContext.Values["ScheduleAvailability"] = options;
                await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                return await stepContext.PromptAsync(nameof(CustomChoicePrompt), promptOptions, cancellationToken);
            }
        }

        private async Task<DialogTurnResult> HandleScheduleAvailabilitySpecificAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userProfile = await _userStateAccessor.GetAsync(stepContext.Context, () => new UserProfileModel(), cancellationToken);
            var cancellationReason = stepContext.Result as dynamic;
            try
            {
                if (cancellationReason.Reason != null && cancellationReason.Reason == DialogReason.CancelCalled)
                {
                    await ResetUserProfile(stepContext, cancellationToken);
                    return await stepContext.EndDialogAsync();
                }
            }
            catch (Exception e) { };
            var choice = (FoundChoice)stepContext.Result;
            if (choice != null && choice.Value.Equals("Atras", StringComparison.OrdinalIgnoreCase))
            {
                stepContext.Values.Remove("DataCita");
                stepContext.Context.Activity.Text = null;
                await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                return await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), null, cancellationToken);
            }
            else if (choice.Value.Equals("Siguiente Página", StringComparison.OrdinalIgnoreCase))
            {
                userProfile.CurrentPage++;
                await _userStateAccessor.SetAsync(stepContext.Context, userProfile, cancellationToken);
                await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                return await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), null, cancellationToken);
            }
            else if (choice.Value.Equals("Pagina Anterior", StringComparison.OrdinalIgnoreCase))
            {
                userProfile.CurrentPage--;
                await _userStateAccessor.SetAsync(stepContext.Context, userProfile, cancellationToken);
                await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                return await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), null, cancellationToken);
            }
            var options = (IEnumerable<dynamic>)stepContext.Values["ScheduleAvailability"];
            var selectedOption = options.FirstOrDefault(option => option.DateTime == choice.Value);

            if (selectedOption != null)
            {
                stepContext.Values["DataCitaHour"] = selectedOption;
                return await stepContext.NextAsync();
            }
            else
            {
                await stepContext.Context.SendActivityAsync("Opción seleccionada no válida.", cancellationToken: cancellationToken);
                return await stepContext.EndDialogAsync();
            }
        }

        private async Task<DialogTurnResult> GetConfirmationAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var dialogState = await _dialogStateAccessor.GetAsync(stepContext.Context, () => new DialogState(), cancellationToken);
            var userProfile = await _userStateAccessor.GetAsync(stepContext.Context, () => new UserProfileModel(), cancellationToken);
            try
            {
                var options = stepContext.Values["DataCita"];
                var promptOptions = new PromptOptions
                {
                    Prompt = MessageFactory.Text("Confirmamos tu cita para el día: " + (string)options.GetType().GetProperty("DateTime").GetValue(options)),
                    Choices = new List<Choice>
                    {
                        new Choice { Value = "Si" },
                        new Choice { Value = "No" }
                    },
                    Style = ListStyle.List
                };
                var option = stepContext.Values["DataCita"];
                await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                return await stepContext.PromptAsync(nameof(CustomChoicePrompt), promptOptions, cancellationToken);

            }
            catch (Exception ex)
            {
                await stepContext.Context.SendActivityAsync("Error en la consulta");
                return await stepContext.EndDialogAsync();
            }

        }

        private async Task<DialogTurnResult> HandleConfirmationCitaAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var options = (IEnumerable<dynamic>)stepContext.Values["ScheduleAvailability"];
            var userProfile = await _userStateAccessor.GetAsync(stepContext.Context, () => new UserProfileModel(), cancellationToken);
            var choice = (FoundChoice)stepContext.Result;
            var selectedOption = options.FirstOrDefault(option => option.DateTime == choice.Value);
            if (choice != null && choice.Value.Equals("Atras", StringComparison.OrdinalIgnoreCase))
            {
                stepContext.Values.Remove("DataCitaHour");
                userProfile.AvailabilityChoice = null;
                return await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), null, cancellationToken);
            }
            if (choice.Value.Equals("Si", StringComparison.OrdinalIgnoreCase))
            {
                await stepContext.Context.SendActivityAsync("Agendando cita...");
                string result = await _apiCalls.PostCreateCitaAsync(userProfile, stepContext.Values["DataCita"], stepContext.Values["DataCitaHour"]);
                await Task.Delay(3000);
                if (result == "true")
                {
                    await stepContext.Context.SendActivityAsync("Cita agendada correctamente.");
                    DataValidation validator = new();
                    string nameFormated;
                    List<string> names = await _apiCalls.GetPreparationAsync(userProfile);
                    if (names != null)
                    {
                        foreach (string name in names)
                        {
                            nameFormated = validator.HtmlCleaner(name);
                            await stepContext.Context.SendActivityAsync(nameFormated);
                        }
                    }
                    await stepContext.Context.SendActivityAsync("Informacion de tu cita:\n\n Fecha: "+ (string)stepContext.Values["DataCita"].GetType().GetProperty("DateTime").GetValue(stepContext.Values["DataCita"])+ "\n\n Doctor: "+ (string)stepContext.Values["DataCita"].GetType().GetProperty("DoctorName").GetValue(stepContext.Values["DataCita"]));
                    await ResetUserProfile(stepContext);
                    await stepContext.Context.SendActivityAsync("¿Te podemos ayudar en algo mas?");
                    return await stepContext.EndDialogAsync();
                }else if (result.Equals("limited"))
                {
                    await ResetUserProfile(stepContext);
                    await stepContext.Context.SendActivityAsync("No hay cupos disponibles para el Mes, aseguradora, plan y la fecha seleccionada, no es posible crear la cita.");
                    return await stepContext.EndDialogAsync();
                }
                else
                {
                    await ResetUserProfile(stepContext);
                    await stepContext.Context.SendActivityAsync("Error al generar la cita. Intenta mas tarde. ⚠️");
                    return await stepContext.EndDialogAsync();
                }
            }
            else if (choice.Value.Equals("No", StringComparison.OrdinalIgnoreCase))
            {
                await _userState.ClearStateAsync(stepContext.Context);
                stepContext.Values.Clear();
                await ResetUserProfile(stepContext);
                await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                await stepContext.Context.SendActivityAsync("Agendamiento cancelado. ¿Te podemos ayudar en algo mas?");
                return await stepContext.NextAsync();
            }
            else
            {
                return await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), null, cancellationToken);
            }
        }
        private Task<bool> ChoiceValidatorAsync(PromptValidatorContext<FoundChoice> promptContext, CancellationToken cancellationToken)
        {
            var selectedOption = promptContext.Recognized.Value;
            if (selectedOption == null || string.IsNullOrWhiteSpace(selectedOption.Value))
            {
                promptContext.Context.SendActivityAsync("Por favor, selecciona una opción válida.", cancellationToken: cancellationToken);
                return Task.FromResult(false); // Indicar que la validación falló
            }
            return Task.FromResult(true);
        }


        protected async override Task<DialogTurnResult> OnContinueDialogAsync(DialogContext innerDc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var userProfile = await _userStateAccessor.GetAsync(innerDc.Context, () => new UserProfileModel(), cancellationToken);
            await _userStateAccessor.SetAsync(innerDc.Context, userProfile);
            await _userState.SaveChangesAsync(innerDc.Context, false, cancellationToken);
            if (userProfile.SaveData)
            {
                string message = await ValidateData(innerDc.Context.Activity.Text.ToString(), innerDc, cancellationToken);
                if (message != "")
                {
                    await innerDc.Context.SendActivityAsync(message, cancellationToken: cancellationToken);
                    return new DialogTurnResult(DialogTurnStatus.Waiting, nameof(WaterfallDialog));
                }
            }
            // Llama al método base para asegurarte de que se ejecuten otras implementaciones de OnContinueDialogAsync en la pila de diálogos
            return await base.OnContinueDialogAsync(innerDc, cancellationToken);
        }

        private async Task<string> ValidateData(string data, DialogContext Context, CancellationToken cancellationToken = default)
        {
            var userProfile = await _userStateAccessor.GetAsync(Context.Context, () => new UserProfileModel(), cancellationToken);
            string message;
            DataValidation dataValidation = new();
            if (string.IsNullOrEmpty(userProfile.DocumentId))
            {
                message = dataValidation.ValidateDocument(data);
                if (message == "")
                {
                    userProfile.DocumentId = data;
                }
            }
            else
            {
                message = "";
            }
            await _userState.SaveChangesAsync(Context.Context, false, cancellationToken);
            return message; ;
        }

        private async Task ResetUserProfile(DialogContext context, CancellationToken cancellationToken = default)
        {
            var userProfile = await _userStateAccessor.GetAsync(context.Context, () => new UserProfileModel(), cancellationToken);

            userProfile.UserId = null;
            userProfile.Name = null;
            userProfile.especialidad = null;
            userProfile.DocumentId = null;
            userProfile.DocumentType = null;
            userProfile.Aseguradora = null;
            userProfile.PlanAseguradora = null;
            userProfile.Servicios = null;
            userProfile.IsNewUser = false;
            userProfile.TypeConsult = null;
            userProfile.ServiceName = null;

            await _userStateAccessor.SetAsync(context.Context, userProfile, cancellationToken);
            await _userState.SaveChangesAsync(context.Context, false, cancellationToken);
        }
    }
}
