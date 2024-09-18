using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using CoreBotTestDD.Models;
using CoreBotTestDD.Services;
using Microsoft.Extensions.Logging;
using CoreBotTestDD.Dialogs;
using System;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using DonBot.Utilities;
using System.Diagnostics;

namespace DonBot.Dialogs
{
    public class RegisterDialog : ComponentDialog
    {
        private readonly UserState _userState;
        private readonly ApiCalls _apiCalls;
        private readonly ConversationState _conversationState;
        private readonly ILogger _logger;
        private readonly IStatePropertyAccessor<DialogState> _dialogStateAccessor;
        private readonly IStatePropertyAccessor<UserProfileModel> _userStateAccessor;

        public RegisterDialog(ILogger<MainDialog> logger, UserState userState, ConversationState conversationState, ApiCalls apiCalls, CustomChoicePrompt customChoicePrompt)
           : base(nameof(RegisterDialog))
        {
            _userState = userState ?? throw new ArgumentNullException(nameof(userState));
            _conversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));
            _logger = logger;
            _apiCalls = apiCalls;
            _dialogStateAccessor = _userState.CreateProperty<DialogState>("DialogState");
            _userStateAccessor = _userState.CreateProperty<UserProfileModel>("UserProfile");

            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt), ChoiceValidatorAsync));
            AddDialog(customChoicePrompt);


            var waterfallSteps = new WaterfallStep[]
            {
                IntroStepAsync,
                HandleRegisterSelectionAsync,
                GetNameAsync,
                GetLastNameAsync,
                GetPhoneAsync,
                GetBirthDayAsync,
                GetGenderAsync,
                GetEmailAsync,
                GetEntitieAsync,
                GetAseguradoraAsync,
                HandleInsuranceSelectionAsync,
                GetAseguradoraPlanAsync,
                GetMaritalStatusAsync,
                GetAfiliationTypeAsync,
                GetPatientTypeAsync,
                HandlePatientTypeAsync,
                GetCityNameAsync,
                GetCityAsync,
                HandleCitySelectionAsync,
                GetAddressAsync,
                GetConfirmationAsync,
                HandleCreateUserSelectionAsync,
                HandleCorrectionDataAsync
            };

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> IntroStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userProfile = await _userStateAccessor.GetAsync(stepContext.Context, () => new UserProfileModel(), cancellationToken);
            userProfile.SaveData = false;
            if (userProfile.Choice == true)
            {
                return await stepContext.NextAsync();
            }
            var choices = new List<Choice>
        {
            new Choice { Value = "Si" },
            new Choice { Value = "No" }
        };
            var promptOptions = new PromptOptions
            {
                Prompt = MessageFactory.Text("¿Deseas Registrate?"),
                Choices = choices,
                Style = ListStyle.List
            };
            stepContext.Values["Choice"] = choices;
            await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
            return await stepContext.PromptAsync(nameof(CustomChoicePrompt), promptOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> HandleRegisterSelectionAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userProfile = await _userStateAccessor.GetAsync(stepContext.Context, () => new UserProfileModel(), cancellationToken);
            if (userProfile.Choice == true)
            {
                return await stepContext.NextAsync();
            }
            var options = (IEnumerable<dynamic>)stepContext.Values["Choice"];
            var choice = (FoundChoice)stepContext.Result;
            if (choice != null)
            {
                if (choice.Value.Equals("No", StringComparison.OrdinalIgnoreCase))
                {
                    stepContext.Context.Activity.Text = null;
                    userProfile.Choice = false;
                    await _conversationState.SaveChangesAsync(stepContext.Context);
                    await stepContext.Context.SendActivityAsync("¿Puedo ayudarte en algo más?", cancellationToken: cancellationToken);
                    var cancellationReason = new { Reason = DialogReason.CancelCalled };
                    await stepContext.CancelAllDialogsAsync(cancellationToken);
                    return await stepContext.EndDialogAsync(cancellationReason, cancellationToken);
                }
                else
                {
                    userProfile.Choice = true;
                    await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                    return await stepContext.NextAsync();
                }

            }
            else
            {
                await stepContext.Context.SendActivityAsync("Opción seleccionada no válida.", cancellationToken: cancellationToken);
                return await stepContext.EndDialogAsync();
            }
        }


        private async Task<DialogTurnResult> GetNameAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userProfile = await _userStateAccessor.GetAsync(stepContext.Context, () => new UserProfileModel(), cancellationToken);
            userProfile.SaveData = true;
            if (userProfile.Name != null)
            {
                userProfile.SaveData = false;
                await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                return await stepContext.NextAsync();
            }
            await stepContext.Context.SendActivityAsync("¿Cual es el nombre del paciente?. Sin apellidos. Escribe unicamente el primer nombre, Ejemplo: Juan, Maria, Laura");
            await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
            return new DialogTurnResult(DialogTurnStatus.Waiting);
        }

        private async Task<DialogTurnResult> GetLastNameAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userProfile = await _userStateAccessor.GetAsync(stepContext.Context, () => new UserProfileModel(), cancellationToken);
            if (userProfile.LastName != null)
            {
                userProfile.SaveData = false;
                await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                return await stepContext.NextAsync();
            }
            if (stepContext.Context.Activity.Text != null && stepContext.Context.Activity.Text.ToString().Equals("Atras", StringComparison.OrdinalIgnoreCase))
            {
                userProfile.Name = null;
                stepContext.Context.Activity.Text = null;
                await _conversationState.SaveChangesAsync(stepContext.Context);
                await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                return await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), null, cancellationToken);
            }
            userProfile.SaveData = true;
            await stepContext.Context.SendActivityAsync("¿Cual es el apellido del paciente? Escribe unicamente el primer apellido, Ejemplo: Rodriguez, Hernandez, Lopez");
            await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
            return new DialogTurnResult(DialogTurnStatus.Waiting);
        }
        private async Task<DialogTurnResult> GetPhoneAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userProfile = await _userStateAccessor.GetAsync(stepContext.Context, () => new UserProfileModel(), cancellationToken);
            if (userProfile.Phone != null)
            {
                userProfile.SaveData = false;
                await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                return await stepContext.NextAsync();
            }
            if (stepContext.Context.Activity.Text != null && stepContext.Context.Activity.Text.ToString().Equals("Atras", StringComparison.OrdinalIgnoreCase))
            {
                userProfile.Name = null;
                stepContext.Context.Activity.Text = null;
                await _conversationState.SaveChangesAsync(stepContext.Context);
                await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                return await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), null, cancellationToken);
            }
            userProfile.SaveData = true;
            await stepContext.Context.SendActivityAsync("¿Cual es tu numero de celular? Recuerda que debe ir sin puntos, comas o espacios. (Ejemplo: 3102436543)");
            await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
            return new DialogTurnResult(DialogTurnStatus.Waiting);
        }

        private async Task<DialogTurnResult> GetBirthDayAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userProfile = await _userStateAccessor.GetAsync(stepContext.Context, () => new UserProfileModel(), cancellationToken);
            if (userProfile.birthdate != null)
            {
                userProfile.SaveData = false;
                await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                return await stepContext.NextAsync();
            }
            if (stepContext.Context.Activity.Text != null && stepContext.Context.Activity.Text.ToString().Equals("Atras", StringComparison.OrdinalIgnoreCase))
            {
                userProfile.LastName = null;
                stepContext.Context.Activity.Text = null;
                await _conversationState.SaveChangesAsync(stepContext.Context);
                await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                return await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), null, cancellationToken);
            }
            userProfile.SaveData = true;
            await stepContext.Context.SendActivityAsync("¿Cual es tu fecha de nacimiento en formato AAAA-MM-DD? Ejemplo: 1992-05-23");
            await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
            return new DialogTurnResult(DialogTurnStatus.Waiting);
        }

        private async Task<DialogTurnResult> GetGenderAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userProfile = await _userStateAccessor.GetAsync(stepContext.Context, () => new UserProfileModel(), cancellationToken);
            if (userProfile.Gender != null)
            {
                userProfile.SaveData = false;
                await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                return await stepContext.NextAsync();
            }
            if (stepContext.Context.Activity.Text != null && stepContext.Context.Activity.Text.ToString().Equals("Atras", StringComparison.OrdinalIgnoreCase))
            {
                userProfile.Phone = null;
                stepContext.Context.Activity.Text = null;
                await _conversationState.SaveChangesAsync(stepContext.Context);
                await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                return await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), null, cancellationToken);
            }
            userProfile.SaveData = false;
            try
            {
                List<JObject> generos = await _apiCalls.GetGenderListAsync(userProfile.CodeCompany);
                if (generos == null || !generos.Any())
                {
                    await stepContext.Context.SendActivityAsync("Error en la consulta de generos.", cancellationToken: cancellationToken);
                    return await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), null, cancellationToken);
                }
                else
                {
                    var options = generos.Select(documentType => new
                    {
                        Id = documentType["genreID"].ToString(),
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
                        Prompt = MessageFactory.Text("Selecciona tu genero:"),
                        Choices = ChoiceFactory.ToChoices(optionsList.Select(option => option.Name).ToList()),
                        Style = ListStyle.List
                    };
                    stepContext.Values["Genders"] = optionsList;
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

        private async Task<DialogTurnResult> GetEmailAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userProfile = await _userStateAccessor.GetAsync(stepContext.Context, () => new UserProfileModel(), cancellationToken);
            userProfile.SaveData = true;
            if (userProfile.Email != null)
            {
                return await stepContext.NextAsync();
            }
            var choice = (FoundChoice)stepContext.Result;
            if (choice != null && choice.Value.Equals("Atras", StringComparison.OrdinalIgnoreCase))
            {
                userProfile.SaveData = false;
                userProfile.birthdate = null;
                stepContext.Context.Activity.Text = null;
                await _conversationState.SaveChangesAsync(stepContext.Context);
                await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                return await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), null, cancellationToken);
            }

            userProfile.SaveData = true;
            if (userProfile.Gender == null)
            {
                var options = (IEnumerable<dynamic>)stepContext.Values["Genders"];
                var selectedOption = options.FirstOrDefault(option => option.Name == choice.Value);
                if (selectedOption != null)
                {
                    userProfile.Gender = selectedOption.Id;
                }
                else
                {
                    await stepContext.Context.SendActivityAsync("Opción seleccionada no válida.", cancellationToken: cancellationToken);
                    return await stepContext.EndDialogAsync();
                }
            }

            await stepContext.Context.SendActivityAsync("Escribe tu correo electronico");
            await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
            return new DialogTurnResult(DialogTurnStatus.Waiting);
        }

        private async Task<DialogTurnResult> GetEntitieAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userProfile = await _userStateAccessor.GetAsync(stepContext.Context, () => new UserProfileModel(), cancellationToken);
            userProfile.SaveData = false;
            if (userProfile.Aseguradora != null)
            {
                return await stepContext.NextAsync();
            }
            if (stepContext.Context.Activity.Text != null && stepContext.Context.Activity.Text.ToString().Equals("Atras", StringComparison.OrdinalIgnoreCase))
            {
                userProfile.Gender = null;
                stepContext.Context.Activity.Text = null;
                await _conversationState.SaveChangesAsync(stepContext.Context);
                await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                return await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), null, cancellationToken);
            }
            await stepContext.Context.SendActivityAsync("¿Con que entidad de salud deseas registrarte para solicitar tu cita? Escribe solamente el nombre de tu EPS, plan complementario, medicina prepagada u otros. Ejemplo: Famisanar, Sura, Compensar.");
            await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
            return new DialogTurnResult(DialogTurnStatus.Waiting);
        }

        private async Task<DialogTurnResult> GetAseguradoraAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var dialogState = await _dialogStateAccessor.GetAsync(stepContext.Context, () => new DialogState(), cancellationToken);
            stepContext.Values["DialogState"] = dialogState;
            var userProfile = await _userStateAccessor.GetAsync(stepContext.Context, () => new UserProfileModel(), cancellationToken);
            if (stepContext.Context.Activity.Text != null && stepContext.Context.Activity.Text.ToString().Equals("Atras", StringComparison.OrdinalIgnoreCase))
            {
                userProfile.Email = null;
                stepContext.Context.Activity.Text = null;
                await _conversationState.SaveChangesAsync(stepContext.Context);
                await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                return await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), null, cancellationToken);
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

        private async Task<DialogTurnResult> GetMaritalStatusAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userProfile = await _userStateAccessor.GetAsync(stepContext.Context, () => new UserProfileModel(), cancellationToken);
            if (userProfile.MaritalStatus != null)
            {
                return await stepContext.NextAsync();
            }
            userProfile.SaveData = false;
            if (stepContext.Context.Activity.Text != null && stepContext.Context.Activity.Text.ToString().Equals("Atras", StringComparison.OrdinalIgnoreCase))
            {
                userProfile.Aseguradora = null;
                stepContext.Context.Activity.Text = null;
                await _conversationState.SaveChangesAsync(stepContext.Context);
                await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                return await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), null, cancellationToken);
            }
            if (userProfile.PlanAseguradora == null)
            {
                var optionsAn = (IEnumerable<dynamic>)stepContext.Values["InsurancesPlan"];
                var choice = (FoundChoice)stepContext.Result;
                var selectedOption = optionsAn.FirstOrDefault(option => option.Name == choice.Value);
                userProfile.PlanAseguradora = selectedOption.Id;
            }
            try
            {
                List<JObject> Estados = await _apiCalls.GetMaritalStatusAsync(userProfile.CodeCompany);
                if (Estados == null || !Estados.Any())
                {
                    await stepContext.Context.SendActivityAsync("Error en la consulta de estados civiles.", cancellationToken: cancellationToken);
                    return await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), null, cancellationToken);
                }
                else
                {
                    var options = Estados.Select(documentType => new
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
                        Prompt = MessageFactory.Text("Selecciona tu estado civil:"),
                        Choices = ChoiceFactory.ToChoices(optionsList.Select(option => option.Name).ToList()),
                        Style = ListStyle.List
                    };
                    stepContext.Values["MaritalStatus"] = optionsList;
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

        private async Task<DialogTurnResult> GetAfiliationTypeAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userProfile = await _userStateAccessor.GetAsync(stepContext.Context, () => new UserProfileModel(), cancellationToken);
            if (userProfile.Affiliation != null)
            {
                await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                return await stepContext.NextAsync();
            }
            if (stepContext.Context.Activity.Text != null && stepContext.Context.Activity.Text.ToString().Equals("Atras", StringComparison.OrdinalIgnoreCase))
            {
                userProfile.PlanAseguradora = null;
                stepContext.Context.Activity.Text = null;
                await _conversationState.SaveChangesAsync(stepContext.Context);
                await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                return await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), null, cancellationToken);
            }
            if (userProfile.MaritalStatus == null)
            {
                var options = (IEnumerable<dynamic>)stepContext.Values["MaritalStatus"];
                var choice = (FoundChoice)stepContext.Result;
                var selectedOption = options.FirstOrDefault(option => option.Name == choice.Value);
                userProfile.MaritalStatus = selectedOption.Id;
            }
            try
            {
                List<JObject> AfiliationType = await _apiCalls.GetAfiliationTypeAsync(userProfile.CodeCompany);
                if (AfiliationType == null || !AfiliationType.Any())
                {
                    await stepContext.Context.SendActivityAsync("Error en la consulta de tipos de afiliacion", cancellationToken: cancellationToken);
                    await stepContext.Context.SendActivityAsync("¿Puedo ayudarte en algo mas? ", cancellationToken: cancellationToken);
                    return await stepContext.EndDialogAsync();
                }
                else
                {
                    var optionsAf = AfiliationType.Select(documentType => new
                    {
                        Id = documentType["id"].ToString(),
                        Name = documentType["name"].ToString()
                    }).ToArray(); var optionsList = optionsAf.ToList();
                    var newOption = new
                    {
                        Id = "Atras",
                        Name = "Atras"
                    };

                    optionsList.Add(newOption);
                    var promptOptions = new PromptOptions
                    {
                        Prompt = MessageFactory.Text("Selecciona tu tipo de afiliacion:"),
                        Choices = ChoiceFactory.ToChoices(optionsList.Select(option => option.Name).ToList()),
                        Style = ListStyle.List
                    };
                    stepContext.Values["AfiliationType"] = optionsList;
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

        private async Task<DialogTurnResult> GetPatientTypeAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userProfile = await _userStateAccessor.GetAsync(stepContext.Context, () => new UserProfileModel(), cancellationToken);
            var choice = (FoundChoice)stepContext.Result;
            if (choice != null && choice.Value.Equals("Atras", StringComparison.OrdinalIgnoreCase))
            {
                userProfile.MaritalStatus = null;
                stepContext.Context.Activity.Text = null;
                await _conversationState.SaveChangesAsync(stepContext.Context);
                await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                return await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), null, cancellationToken);
            }
            if (userProfile.Affiliation == null)
            {
                var options = (IEnumerable<dynamic>)stepContext.Values["AfiliationType"];
                var selectedOption = options.FirstOrDefault(option => option.Name == choice.Value);
                if (selectedOption != null)
                {
                    userProfile.Affiliation = selectedOption.Id;
                }
                else
                {
                    await stepContext.Context.SendActivityAsync("Opción seleccionada no válida.", cancellationToken: cancellationToken);
                    return await stepContext.EndDialogAsync();
                }
            }
            if (userProfile.PatientType != null)
            {
                userProfile.SaveData = false;
                await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                return await stepContext.NextAsync();
            }
            try
            {
                List<JObject> Tipos = await _apiCalls.GetPatientTypeListAsync(userProfile.CodeCompany);
                if (Tipos == null || !Tipos.Any())
                {
                    await stepContext.Context.SendActivityAsync("No se encontraron tipos de pacientes.", cancellationToken: cancellationToken);
                    return await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), null, cancellationToken);
                }
                else
                {
                    List<string> TypeList = new List<string>();
                    foreach (var servicio in Tipos)
                    {
                        TypeList.Add(servicio.Value<string>("name"));
                    }
                    var options = Tipos.Select(documentType => new
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
                        Prompt = MessageFactory.Text("Escoge el tipo de paciente:"),
                        Choices = ChoiceFactory.ToChoices(optionsList.Select(option => option.Name).ToList()),
                        Style = ListStyle.List
                    };
                    stepContext.Values["Pacientes"] = optionsList;
                    await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                    return await stepContext.PromptAsync(nameof(ChoicePrompt), promptOptions, cancellationToken);
                }

            }
            catch (Exception ex)
            {
                await stepContext.Context.SendActivityAsync("Error en la consulta");
                return await stepContext.EndDialogAsync();
            }
        }


        private async Task<DialogTurnResult> HandlePatientTypeAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userProfile = await _userStateAccessor.GetAsync(stepContext.Context, () => new UserProfileModel(), cancellationToken);
            var choice = (FoundChoice)stepContext.Result;
            if (userProfile.PatientType != null)
            {
                userProfile.SaveData = false;
                await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                return await stepContext.NextAsync();
            }
            var options = (IEnumerable<dynamic>)stepContext.Values["Pacientes"];
            var selectedOption = options.FirstOrDefault(option => option.Name == choice.Value);
            if (selectedOption != null)
            {
                if (choice != null && choice.Value.Equals("Atras", StringComparison.OrdinalIgnoreCase))
                {
                    userProfile.Affiliation = null;
                    stepContext.Context.Activity.Text = null;
                    await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                    return await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), null, cancellationToken);
                }
                userProfile.PatientType = selectedOption.Id;
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

        private async Task<DialogTurnResult> GetCityNameAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userProfile = await _userStateAccessor.GetAsync(stepContext.Context, () => new UserProfileModel(), cancellationToken);
            if (userProfile.City != null)
            {
                return await stepContext.NextAsync();
            }
            await stepContext.Context.SendActivityAsync("Escribe tu ciudad de residencia");
            await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
            return new DialogTurnResult(DialogTurnStatus.Waiting);
        }

        private async Task<DialogTurnResult> GetCityAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var dialogState = await _dialogStateAccessor.GetAsync(stepContext.Context, () => new DialogState(), cancellationToken);
            stepContext.Values["DialogState"] = dialogState;
            var userProfile = await _userStateAccessor.GetAsync(stepContext.Context, () => new UserProfileModel(), cancellationToken);
            if (stepContext.Context.Activity.Text != null && stepContext.Context.Activity.Text.ToString().Equals("Atras", StringComparison.OrdinalIgnoreCase))
            {
                userProfile.PatientType = null;
                stepContext.Context.Activity.Text = null;
                await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                return await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), null, cancellationToken);
            }
            if (userProfile.City != null)
            {
                return await stepContext.NextAsync();
            };
            try
            {
                List<JObject> ciudades = await _apiCalls.GetCityListAsync(userProfile.CodeCompany, stepContext.Context.Activity.Text.ToString());
                if (ciudades == null || !ciudades.Any())
                {
                    await stepContext.Context.SendActivityAsync("No se encontraron ciudades coincidentes.", cancellationToken: cancellationToken);
                    return await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), null, cancellationToken);
                }
                else
                {
                    List<string> servicios = new List<string>();
                    foreach (var servicio in ciudades)
                    {
                        servicios.Add(servicio.Value<string>("name"));
                    }
                    if (servicios.Count == 1)
                    {
                        userProfile.City = ciudades[0]["cityID"].ToString();
                        await _userStateAccessor.SetAsync(stepContext.Context, userProfile, cancellationToken);
                        await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                        return await stepContext.NextAsync();
                    }
                    var options = ciudades.Select(documentType => new
                    {
                        Id = documentType["cityID"].ToString(),
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
                        Prompt = MessageFactory.Text("Encontramos estos resultados:"),
                        Choices = ChoiceFactory.ToChoices(optionsList.Select(option => option.Name).ToList()),
                        Style = ListStyle.List
                    };
                    stepContext.Values["Ciudades"] = optionsList;
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

        private async Task<DialogTurnResult> HandleCitySelectionAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            var choice = (FoundChoice)stepContext.Result;
            var userProfile = await _userStateAccessor.GetAsync(stepContext.Context, () => new UserProfileModel(), cancellationToken);
            if (userProfile.City != null)
            {
                return await stepContext.NextAsync();
            }
            var options = (IEnumerable<dynamic>)stepContext.Values["Ciudades"];
            var selectedOption = options.FirstOrDefault(option => option.Name == choice.Value);
            if (selectedOption != null)
            {
                if (choice != null && choice.Value.Equals("Atras", StringComparison.OrdinalIgnoreCase))
                {
                    userProfile.City = null;
                    stepContext.Context.Activity.Text = null;
                    await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                    return await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), null, cancellationToken);
                }
                userProfile.City = selectedOption.Id;
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

        private async Task<DialogTurnResult> GetAddressAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userProfile = await _userStateAccessor.GetAsync(stepContext.Context, () => new UserProfileModel(), cancellationToken);
            userProfile.SaveData = true;
            if (userProfile.Address != null)
            {
                userProfile.SaveData = false;
                await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                return await stepContext.NextAsync();
            }
            await stepContext.Context.SendActivityAsync("Por favor, escriba tu direccion: ");
            await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
            return new DialogTurnResult(DialogTurnStatus.Waiting);
        }

        private async Task<DialogTurnResult> GetConfirmationAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userProfile = await _userStateAccessor.GetAsync(stepContext.Context, () => new UserProfileModel(), cancellationToken);
            if(userProfile.dataCorrection == true)
            {
                return await stepContext.NextAsync();
            }
            if (stepContext.Context.Activity.Text != null && stepContext.Context.Activity.Text.ToString().Equals("Atras", StringComparison.OrdinalIgnoreCase))
            {
                userProfile.City = null;
                stepContext.Context.Activity.Text = null;
                await _conversationState.SaveChangesAsync(stepContext.Context);
                await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                return await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), null, cancellationToken);
            }
            string message = await _apiCalls.GetUserDataCreationAsync(userProfile);
            await stepContext.Context.SendActivityAsync(message);
            var choices = new List<Choice>
        {
            new Choice { Value = "Si" },
            new Choice { Value = "No" },
            new Choice { Value = "Corregir datos"},
            new Choice { Value = "Cancelar"}
        };
            var promptOptions = new PromptOptions
            {
                Prompt = MessageFactory.Text("¿Deseas proceder a registrar el usuario?"),
                Choices = choices,
                Style = ListStyle.List
            };
            stepContext.Values["Choice"] = choices;
            await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
            return await stepContext.PromptAsync(nameof(ChoicePrompt), promptOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> HandleCreateUserSelectionAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userProfile = await _userStateAccessor.GetAsync(stepContext.Context, () => new UserProfileModel(), cancellationToken);
            var choice = (FoundChoice)stepContext.Result;
            if (choice.Value != null && choice.Value.Equals("Atras", StringComparison.OrdinalIgnoreCase) && userProfile.dataCorrection == false)
            {
                stepContext.Context.Activity.Text = null;
                await _conversationState.SaveChangesAsync(stepContext.Context);
                await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                return await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), null, cancellationToken);
            }
            else if (choice.Value != null && choice.Value.Equals("Cancelar", StringComparison.OrdinalIgnoreCase))
            {
                await stepContext.Context.SendActivityAsync("Proceso cancelado.");
                await stepContext.Context.SendActivityAsync("¿Te podemos ayudar en algo mas?");
                var cancellationReason = new { Reason = DialogReason.CancelCalled };
                await stepContext.CancelAllDialogsAsync(cancellationToken);
                await ResetUserProfile(stepContext, cancellationToken);
                return await stepContext.EndDialogAsync(cancellationReason, cancellationToken);
            }
            else if ((choice.Value != null && choice.Value.Equals("Corregir datos", StringComparison.OrdinalIgnoreCase)))
                 {
                var choices = new List<Choice>
                 {
                 new Choice { Value = "Documento" },
                 new Choice { Value = "Nombre" },
                 new Choice { Value = "Apellido"},
                 new Choice { Value = "Telefono"},
                 new Choice { Value = "Fecha de nacimiento"},
                 new Choice { Value = "Genero"},
                 new Choice { Value = "Correo"},
                 new Choice { Value = "Aseguradora"},
                 new Choice { Value = "Plan de aseguradora"},
                 new Choice { Value = "Estado civil"},
                 new Choice { Value = "Tipo de afiliacion"},
                 new Choice { Value = "Ciudad"},
                 new Choice { Value = "Tipo de paciente"},
                 new Choice { Value = "Direccion"},
                 new Choice { Value = "Atras"},
                };
                userProfile.dataCorrection = true;
                var promptOptions = new PromptOptions
                {
                    Prompt = MessageFactory.Text("¿Que dato deseas corregir?"),
                    Choices = choices,
                    Style = ListStyle.List
                };
                stepContext.Values["Choice"] = choices;
                await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                return await stepContext.PromptAsync(nameof(ChoicePrompt), promptOptions, cancellationToken);
            }
            else if (choice.Value.Equals("Si", StringComparison.OrdinalIgnoreCase))
            {
                string response = await _apiCalls.PostCreateUserAsync(userProfile);
                if (response == "3")
                {
                    await stepContext.Context.SendActivityAsync("Este usuario ya existe.");
                    await stepContext.Context.SendActivityAsync(" ¿Te podemos ayudar en algo mas?");
                    var cancellationReason = new { Reason = DialogReason.CancelCalled };
                    await stepContext.CancelAllDialogsAsync(cancellationToken);
                    await ResetUserProfile(stepContext, cancellationToken);
                    return await stepContext.EndDialogAsync(cancellationReason, cancellationToken);
                }
                else if(response == "error")
                {
                    await stepContext.Context.SendActivityAsync("Se ha producido un error en la creacion del usuario, intenta mas tarde.", cancellationToken: cancellationToken);
                    var cancellationReason = new { Reason = DialogReason.CancelCalled };
                    await stepContext.CancelAllDialogsAsync(cancellationToken);
                    await ResetUserProfile(stepContext, cancellationToken);
                    return await stepContext.EndDialogAsync(cancellationReason, cancellationToken);
                }
                else
                {
                    stepContext.Context.Activity.Text = null;
                    userProfile.Choice = false;
                    await _conversationState.SaveChangesAsync(stepContext.Context);
                    await stepContext.Context.SendActivityAsync("Tu usuario ha sido creado con exito, puedes proceder con el agendamiento de la cita.", cancellationToken: cancellationToken);
                    return await stepContext.EndDialogAsync();
                }
            }
            else
            {
                await stepContext.Context.SendActivityAsync(" ¿Te podemos ayudar en algo mas?");
                var cancellationReason = new { Reason = DialogReason.CancelCalled };
                await stepContext.CancelAllDialogsAsync(cancellationToken);
                await ResetUserProfile(stepContext, cancellationToken);
                return await stepContext.EndDialogAsync(cancellationReason, cancellationToken);
            }
        }

        private async Task<DialogTurnResult> HandleCorrectionDataAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userProfile = await _userStateAccessor.GetAsync(stepContext.Context, () => new UserProfileModel(), cancellationToken);
            var choice = (FoundChoice)stepContext.Result;
            if (userProfile.dataCorrection == true)
            {
                var options = (IEnumerable<dynamic>)stepContext.Values["Choice"];
                var selectedOption = options.FirstOrDefault(option => option.Value == choice.Value);
                if (selectedOption != null)
                {
                    if (selectedOption != null && selectedOption.Value.Equals("Atras", StringComparison.OrdinalIgnoreCase))
                    {
                        userProfile.dataCorrection = false;
                        stepContext.Context.Activity.Text = null;
                        await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                        return await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), null, cancellationToken);
                    }
                    userProfile.dataCorrection = false;
                    switch (selectedOption.Value)
                    {
                        case "Documento":
                            userProfile.DocumentType = null;
                            userProfile.DocumentId = null;
                            break;
                        case "Nombre":
                            userProfile.Name = null;
                            break;
                        case "Apellido":
                            userProfile.LastName = null;
                            break;
                        case "Telefono":
                            userProfile.Phone = null;
                            break;
                        case "Fecha de nacimiento":
                            userProfile.birthdate = null;
                            break;
                        case "Genero":
                            userProfile.Gender = null;
                            break;
                        case "Correo":
                            userProfile.Email = null;
                            break;
                        case "Aseguradora":
                            userProfile.Aseguradora = null;
                            break;
                        case "Plan de aseguradora":
                            userProfile.PlanAseguradora = null;
                            break;
                        case "Estado civil":
                            userProfile.MaritalStatus = null;
                            break;
                        case "Tipo de afiliacion":
                            userProfile.Affiliation = null;
                            break;
                        case "Ciudad":
                            userProfile.City = null;
                            break;
                        case "Tipo de paciente":
                            userProfile.PatientType = null;
                            break;
                        case "Direccion":
                            userProfile.Address = null;
                            break;
                        case null:
                            userProfile.dataCorrection = true;
                            break;
                    }
                    await _userStateAccessor.SetAsync(stepContext.Context, userProfile, cancellationToken);
                    await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                    return await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), null, cancellationToken);
                }
                else
                {
                    return await stepContext.EndDialogAsync();
                }
            }
            else
            {
                await stepContext.Context.SendActivityAsync("Opción seleccionada no válida.", cancellationToken: cancellationToken);
                return await stepContext.EndDialogAsync();
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
            if (data.Equals("Atras", StringComparison.OrdinalIgnoreCase))
            {
                return message = "";
            }
            if (string.IsNullOrEmpty(userProfile.Name))
            {
                userProfile.Name = data;
                message = "";
            }
            else if (string.IsNullOrEmpty(userProfile.LastName))
            {
                userProfile.LastName = data;
                message = "";
            }
            else if (string.IsNullOrEmpty(userProfile.Phone))
            {
                message = dataValidation.ValidatePhone(data);
                if (message == "")
                {
                    userProfile.Phone = data;
                }
            }
            else if (string.IsNullOrEmpty(userProfile.birthdate))
            {
                message = dataValidation.ValidateDate(data);
                if (message == "")
                {
                    userProfile.birthdate = data;
                }
            }
            else if (string.IsNullOrEmpty(userProfile.Email))
            {
                message = dataValidation.ValidateEmail(data);
                if (message == "")
                {
                    userProfile.Email = data;
                }
            }
            else if (string.IsNullOrEmpty(userProfile.Address))
            {
                userProfile.Address = data;
                message = "";
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
            userProfile.LastName = null;
            userProfile.DocumentId = null;
            userProfile.DocumentType = null;
            userProfile.Aseguradora = null;
            userProfile.PlanAseguradora = null;
            userProfile.Address = null;
            userProfile.birthdate = null;
            userProfile.Phone = null;
            userProfile.PatientType = null;
            userProfile.Affiliation = null;
            userProfile.City = null;
            userProfile.Email = null;
            userProfile.Gender = null;
            userProfile.MaritalStatus = null;
            userProfile.Choice = false;

            await _userStateAccessor.SetAsync(context.Context, userProfile, cancellationToken);
            await _userState.SaveChangesAsync(context.Context, false, cancellationToken);
        }
    }
}
