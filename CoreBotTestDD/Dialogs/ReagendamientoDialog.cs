using CoreBotTestDD.Dialogs;
using CoreBotTestDD.Models;
using CoreBotTestDD.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Threading;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using DonBot.Utilities;

namespace DonBot.Dialogs
{
    public class ReagendamientoDialog : ComponentDialog
    {
        private readonly UserState _userState;
        private readonly ApiCalls _apiCalls;
        private readonly ConversationState _conversationState;
        private readonly ILogger _logger;
        private readonly IStatePropertyAccessor<DialogState> _dialogStateAccessor;
        private readonly IStatePropertyAccessor<UserProfileModel> _userStateAccessor;
        public ReagendamientoDialog(ILogger<MainDialog> logger, UserState userState, ConversationState conversationState, ApiCalls apiCalls, CustomChoicePrompt customChoicePrompt, RegisterDialog registerDialog)
          : base(nameof(ReagendamientoDialog))
        {
            _userState = userState ?? throw new ArgumentNullException(nameof(userState));
            _conversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));
            _logger = logger;
            _apiCalls = apiCalls;
            _dialogStateAccessor = _userState.CreateProperty<DialogState>("DialogState");
            _userStateAccessor = _userState.CreateProperty<UserProfileModel>("UserProfile");

            AddDialog(registerDialog);
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt), ChoiceValidatorAsync));
            AddDialog(customChoicePrompt);

            var waterfallSteps = new WaterfallStep[]
           {
               IntroStepAsync,
               HandleDocumentTypeSelectionAsync,
               GetDocumentIdAsync,
               HandelDocumentTypeAsync,
               GetConfirmationAsync,
               GetScheduleAvailabilityAsync,
               HandleScheduleAvailabilityAsync,
               GetConfirmationAvalaibilityAsync,
               HandleConfirmationAvalaibilityCitaAsync
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
                        Prompt = MessageFactory.Text("Para proceder, por favor identificate, ¿Cuál es tu tipo y documento de identidad?"),
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
                }
                else if (choice.Value.Equals("Siguiente Página", StringComparison.OrdinalIgnoreCase))
                {
                    userProfile.CurrentPage++;
                    await _userStateAccessor.SetAsync(stepContext.Context, userProfile, cancellationToken);
                    await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                    return await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), null, cancellationToken);
                }
                else if (choice.Value.Equals("Página Anterior", StringComparison.OrdinalIgnoreCase))
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
            await stepContext.Context.SendActivityAsync("Escribe el numero de tu documento sin puntos ni comas. (Ejemplo: 1001122345) Escribe atras si quieres cambiar tu informacion anterior.");
            await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
            return new DialogTurnResult(DialogTurnStatus.Waiting);
        }

        private async Task<DialogTurnResult> HandelDocumentTypeAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
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
            if (userProfile.Name != null)
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
                    if (userProfile.IsNewUser == true)
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
            return await stepContext.NextAsync();

        }

        private async Task<DialogTurnResult> GetConfirmationAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userProfile = await _userStateAccessor.GetAsync(stepContext.Context, () => new UserProfileModel(), cancellationToken);
            var cancellationReason = stepContext.Result as dynamic;
            if(userProfile.Servicios != null)
            {
                return await stepContext.NextAsync();
            }
            try
            {
                if (cancellationReason.Reason != null && cancellationReason.Reason == DialogReason.CancelCalled)
                {
                    await ResetUserProfile(stepContext, cancellationToken);
                    return await stepContext.EndDialogAsync();
                }
            }
            catch { }
            try
            {
                List<JObject> citas = await _apiCalls.GetCitasByPatientAsync(userProfile.UserId, userProfile.CodeCompany);
                if (citas == null || !citas.Any())
                {
                    await stepContext.Context.SendActivityAsync("No se encontraron citas agendadas.", cancellationToken: cancellationToken);
                    await ResetUserProfile(stepContext);
                    await stepContext.Context.SendActivityAsync("¿Te puedo ayudar en algo mas?.", cancellationToken: cancellationToken);
                    return await stepContext.EndDialogAsync();
                }
                else
                {
                    var options = citas.Select(documentType => new
                    {
                        Id = documentType["appointmentID"].ToString(),
                        Name = documentType["appointmentTypeName"].ToString(),
                        PlanId = documentType["insurancePlanID"].ToString(),
                        ServiceId = documentType["serviceID"].ToString(),
                        Especialidad = documentType["specialtyID"].ToString()
                    }).ToArray(); var optionsList = options.ToList();
                    var newOption = new
                    {
                        Id = "Atras",
                        Name = "Atras",
                        PlanId = "Atras",
                        ServiceId = "Atras",
                        Especialidad = "Atras",
                    };
                    var newOptionC = new
                    {
                        Id = "Cancelar",
                        Name = "Cancelar",
                        PlanId = "Cancelar",
                        ServiceId = "Cancelar",
                        Especialidad = "Cancelar",
                    };
                    optionsList.Add(newOption);
                    optionsList.Add(newOptionC);
                    var promptOptions = new PromptOptions
                    {
                        Prompt = MessageFactory.Text("Selecciona la cita a reagendar:"),
                        Choices = ChoiceFactory.ToChoices(optionsList.Select(option => option.Name).ToList()),
                        Style = ListStyle.List
                    };
                    stepContext.Values["Citas"] = optionsList;
                    await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                    return await stepContext.PromptAsync(nameof(CustomChoicePrompt), promptOptions, cancellationToken);
                }
            }
            catch (Exception e)
            {
                await stepContext.Context.SendActivityAsync("Error en la consulta");
                return await stepContext.EndDialogAsync();
            }
        }

        private async Task<DialogTurnResult> GetScheduleAvailabilityAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userProfile = await _userStateAccessor.GetAsync(stepContext.Context, () => new UserProfileModel(), cancellationToken);
            if (userProfile.CitaDate != null)
            {
                return await stepContext.NextAsync();
            }
            if(userProfile.Servicios == null) {
                var optionsAnt = (IEnumerable<dynamic>)stepContext.Values["Citas"];
                var choice = (FoundChoice)stepContext.Result;
                if (choice != null && choice.Value.Equals("Atras", StringComparison.OrdinalIgnoreCase))
                {
                    userProfile.DocumentId = null;
                    userProfile.Name = null;
                    stepContext.Context.Activity.Text = null;
                    return await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), null, cancellationToken);
                }
                if (choice != null && choice.Value.Equals("Cancelar", StringComparison.OrdinalIgnoreCase))
                {
                    await _userState.ClearStateAsync(stepContext.Context);
                    stepContext.Values.Clear();
                    await ResetUserProfile(stepContext);
                    await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                    await stepContext.Context.SendActivityAsync("Reagendamiento cancelado. ¿Te podemos ayudar en algo mas?");
                    return await stepContext.EndDialogAsync();
                }
                var selectedOption = optionsAnt.FirstOrDefault(option => option.Name == choice.Value);
                userProfile.Servicios = selectedOption.ServiceId;
                userProfile.especialidad = selectedOption.Especialidad;
                userProfile.PlanAseguradora = selectedOption.PlanId;
                userProfile.IdCita = selectedOption.Id;
            }
            try
            {
                List<JObject> ScheduleAvailability = await _apiCalls.GetScheduleAvailabilityAsync(userProfile, "0");
                if (ScheduleAvailability == null || !ScheduleAvailability.Any())
                {
                    await stepContext.Context.SendActivityAsync("No se encontro agenda disponible. ", cancellationToken: cancellationToken);
                    stepContext.Context.Activity.Text = null;
                    await _conversationState.SaveChangesAsync(stepContext.Context);
                    return await stepContext.BeginDialogAsync(nameof(ListaEsperaDialog), null, cancellationToken);
                }
                else
                {
                    var options = ScheduleAvailability.Select(documentType => new
                    {
                        DateTime = documentType.GetValue("dateTime").ToString(),
                        UserId = documentType.GetValue("userID").ToString(),
                        AppointmentTypeId = documentType.GetValue("appointmentTypeID").ToString(),
                        Duration = documentType.GetValue("duration").ToString(),
                        OfficeId = documentType.GetValue("officeID").ToString(),
                        DoctorName = documentType.GetValue("doctorName").ToString()
                    }).Take(5).ToArray();
                    var optionsWithBack = options.Concat(new[]
                    {
                     new
                     {
                         DateTime = "Atras",
                            UserId = string.Empty,
                            AppointmentTypeId = string.Empty,
                         Duration = string.Empty,
                         OfficeId = string.Empty,
                         DoctorName = string.Empty
                      }
                    }).ToArray();

                    var promptOptions = new PromptOptions
                    {
                        Prompt = MessageFactory.Text("Selecciona una fecha para tu cita: " + Environment.NewLine),
                        Choices = ChoiceFactory.ToChoices(optionsWithBack.Select(option => option.DateTime).ToList()),
                        Style = ListStyle.List
                    };

                    stepContext.Values["ScheduleAvailability"] = options;
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

        private async Task<DialogTurnResult> HandleScheduleAvailabilityAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userProfile = await _userStateAccessor.GetAsync(stepContext.Context, () => new UserProfileModel(), cancellationToken);
            var cancellationReason = stepContext.Result as dynamic;
            if(userProfile.CitaDate != null)
            {
                return await stepContext.NextAsync();
            }
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
                userProfile.Servicios = null;
                stepContext.Context.Activity.Text = null;
                await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                return await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), null, cancellationToken);
            }
            var options = (IEnumerable<dynamic>)stepContext.Values["ScheduleAvailability"];
            var selectedOption = options.FirstOrDefault(option => option.DateTime == choice.Value);

            if (selectedOption != null)
            {
                userProfile.CitaDate = selectedOption.DateTime;
                stepContext.Values["DataCita"] = selectedOption;
                return await stepContext.NextAsync();
            }
            else
            {
                await stepContext.Context.SendActivityAsync("Opción seleccionada no válida.", cancellationToken: cancellationToken);
                return await stepContext.EndDialogAsync();
            }
        }

        private async Task<DialogTurnResult> GetConfirmationAvalaibilityAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
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
                        new Choice { Value = "No" },
                        new Choice { Value = "Atras" }
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

        private async Task<DialogTurnResult> HandleConfirmationAvalaibilityCitaAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var options = (IEnumerable<dynamic>)stepContext.Values["ScheduleAvailability"];
            var userProfile = await _userStateAccessor.GetAsync(stepContext.Context, () => new UserProfileModel(), cancellationToken);
            var choice = (FoundChoice)stepContext.Result;
            var selectedOption = options.FirstOrDefault(option => option.DateTime == choice.Value);
            if (choice != null && choice.Value.Equals("Atras", StringComparison.OrdinalIgnoreCase))
            {
                userProfile.CitaDate = null;
                stepContext.Context.Activity.Text = null;
                return await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), null, cancellationToken);
            }
            if (choice.Value.Equals("Si", StringComparison.OrdinalIgnoreCase))
            {
                await stepContext.Context.SendActivityAsync("Reagendando tu cita...");
                bool result = await _apiCalls.PutReagendarCitaAsync(stepContext.Values["DataCita"], userProfile.CodeCompany, userProfile.IdCita, userProfile.UserId);
                await Task.Delay(3000);
                if (result == true)
                {
                    await stepContext.Context.SendActivityAsync("Cita reprogramada correctamente.");
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
                    await ResetUserProfile(stepContext);
                    await stepContext.Context.SendActivityAsync("¿Te podemos ayudar en algo mas?");
                    return await stepContext.EndDialogAsync();
                }
                else
                {
                    await ResetUserProfile(stepContext);
                    await stepContext.Context.SendActivityAsync("Error al generar la cita. Intenta mas tarde.");
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
            if (userProfile.SaveData)
            {
                userProfile.DocumentId = innerDc.Context.Activity.Text.ToString();
            }
            await _userStateAccessor.SetAsync(innerDc.Context, userProfile);
            await _userState.SaveChangesAsync(innerDc.Context, false, cancellationToken);
            return await base.OnContinueDialogAsync(innerDc, cancellationToken);
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

            await _userStateAccessor.SetAsync(context.Context, userProfile, cancellationToken);
            await _userState.SaveChangesAsync(context.Context, false, cancellationToken);
        }
    }
}
