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

namespace CoreBotTestDD.Dialogs
{
    public class AgendarDialog : ComponentDialog
    {
        private readonly UserState _userState;
        private readonly ApiCalls _apiCalls;
        private readonly ConversationState _conversationState;
        private readonly ILogger _logger;
        private readonly IStatePropertyAccessor<DialogState> _dialogStateAccessor;
        private readonly IStatePropertyAccessor<UserProfileModel> _userStateAccessor;

        public AgendarDialog(ILogger<MainDialog> logger, UserState userState, ConversationState conversationState, ApiCalls apiCalls)
           : base(nameof(AgendarDialog))
        {
            _userState = userState ?? throw new ArgumentNullException(nameof(userState));
            _conversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));
            _logger = logger;
            _apiCalls = apiCalls;
            _dialogStateAccessor = _userState.CreateProperty<DialogState>("DialogState");
            _userStateAccessor = _userState.CreateProperty<UserProfileModel>("UserProfile");

            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt), ChoiceValidatorAsync));


            var waterfallSteps = new WaterfallStep[]
            {
                IntroStepAsync,
                HandleDocumentTypeSelectionAsync,
                GetDocumentIdAsync,
                GetAseguradoraNameAsync,
                GetAseguradoraAsync,
                HandleInsuranceSelectionAsync,
                GetAseguradoraPlanAsync,
                GetServiceNameAsync,
                GetServiceAsync,
                HandleServiceSelectionAsync,
                GetEspecialidadAsync,
                GetScheduleAvailabilityAsync,
                HandleScheduleAvailabilityAsync,
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
            await stepContext.Context.SendActivityAsync("Muy bien, vamos a agendar una cita.", cancellationToken: cancellationToken);
            var userProfile = await _userStateAccessor.GetAsync(stepContext.Context, () => new UserProfileModel(), cancellationToken);
            if (userProfile.DocumentId != null)
            {
                return await stepContext.NextAsync();
            }
            try
            {
                List<JObject> DocumentTypes = await _apiCalls.GetDocumentTypeAsync();
                if (DocumentTypes == null || !DocumentTypes.Any())
                {
                    await stepContext.Context.SendActivityAsync("Error en la consulta.", cancellationToken: cancellationToken);
                    return await stepContext.EndDialogAsync();
                }
                else
                {
                    var options = DocumentTypes.Select(documentType => new
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
                        Prompt = MessageFactory.Text("Selecciona tu tipo de documento de identidad:"),
                        Choices = ChoiceFactory.ToChoices(optionsList.Select(option => option.Name).ToList()),
                    };
                    stepContext.Values["documentOptions"] = optionsList;
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

        private async Task<DialogTurnResult> HandleDocumentTypeSelectionAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userProfile = await _userStateAccessor.GetAsync(stepContext.Context, () => new UserProfileModel(), cancellationToken);
            if (userProfile.DocumentId != null)
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
                    await stepContext.Context.SendActivityAsync("¿Cuentame, en que te puedo ayudar?", cancellationToken: cancellationToken);
                    return await stepContext.EndDialogAsync();
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
            if(userProfile.DocumentId != null) {
                return await stepContext.NextAsync();
            }
            await stepContext.Context.SendActivityAsync("Escribe el numero de tu documento: ");
            await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
            return new DialogTurnResult(DialogTurnStatus.Waiting);
        }

        private async Task<DialogTurnResult> GetAseguradoraNameAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userProfile = await _userStateAccessor.GetAsync(stepContext.Context, () => new UserProfileModel(), cancellationToken);
            userProfile.Name = await _apiCalls.GetUserByIdAsync(userProfile.DocumentType, userProfile.DocumentId);
            if(userProfile.Name != null)
            {
                await stepContext.Context.SendActivityAsync("Bienvenido " + userProfile.Name);
            }
            await stepContext.Context.SendActivityAsync("¿Con que entidad de salud deseas solicitar la cita? ");
            userProfile.SaveData = false;
            await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
            return new DialogTurnResult(DialogTurnStatus.Waiting);
        }

        private async Task<DialogTurnResult> GetAseguradoraAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var dialogState = await _dialogStateAccessor.GetAsync(stepContext.Context, () => new DialogState(), cancellationToken);
            stepContext.Values["DialogState"] = dialogState;
            var userProfile = await _userStateAccessor.GetAsync(stepContext.Context, () => new UserProfileModel(), cancellationToken);
            if (userProfile.Aseguradora != null)
            {
                return await stepContext.NextAsync();
            }
            try
            {
                List<JObject> aseguradoras = await _apiCalls.GetAseguradorasAsync(stepContext.Context.Activity.Text.ToString());
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
                        Choices = ChoiceFactory.ToChoices(options.Select(option => option.Name).ToList()),
                    };
                    stepContext.Values["Insurances"] = options;
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

        private async Task<DialogTurnResult> HandleInsuranceSelectionAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var options = (IEnumerable<dynamic>)stepContext.Values["Insurances"];
            var userProfile = await _userStateAccessor.GetAsync(stepContext.Context, () => new UserProfileModel(), cancellationToken);
            if (userProfile.Aseguradora != null)
            {
                return await stepContext.NextAsync();
            }
            var choice = (FoundChoice)stepContext.Result;
            var selectedOption = options.FirstOrDefault(option => option.Name == choice.Value);

            if (selectedOption != null)
            {
                if (choice.Value.Equals("Atras", StringComparison.OrdinalIgnoreCase))
                {
                    userProfile.Aseguradora = null;
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
            if (userProfile.Aseguradora != null)
            {
                return await stepContext.NextAsync();
            }
            try
            {
                List<JObject> aseguradoras = await _apiCalls.GetInsurancePlanAsync(userProfile.Aseguradora);
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
                        Choices = ChoiceFactory.ToChoices(options.Select(option => option.Name).ToList()),
                    };
                    stepContext.Values["InsurancesPlan"] = options;
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

        private async Task<DialogTurnResult> GetServiceNameAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var options = (IEnumerable<dynamic>)stepContext.Values["InsurancesPlan"];
            var choice = (FoundChoice)stepContext.Result;
            var selectedOption = options.FirstOrDefault(option => option.Name == choice.Value);
            var userProfile = await _userStateAccessor.GetAsync(stepContext.Context, () => new UserProfileModel(), cancellationToken);
            if (userProfile.Servicios != null)
            {
                return await stepContext.NextAsync();
            }
            userProfile.PlanAseguradora = selectedOption.Id;
            await stepContext.Context.SendActivityAsync("Que servicio deseas agendar?");
            userProfile.SaveData = false;
            await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
            return new DialogTurnResult(DialogTurnStatus.Waiting);
        }

        private async Task<DialogTurnResult> GetServiceAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var dialogState = await _dialogStateAccessor.GetAsync(stepContext.Context, () => new DialogState(), cancellationToken);
            stepContext.Values["DialogState"] = dialogState;
            var userProfile = await _userStateAccessor.GetAsync(stepContext.Context, () => new UserProfileModel(), cancellationToken);
            if (userProfile.Servicios != null)
            {
                return await stepContext.NextAsync();
            };
            try
            {
                List<JObject> Servicios = await _apiCalls.GetServicesAsync(stepContext.Context.Activity.Text.ToString());
                if (Servicios == null || !Servicios.Any())
                {
                    await stepContext.Context.SendActivityAsync("No se encontraron servicios disponibles.", cancellationToken: cancellationToken);
                    return await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), null, cancellationToken);
                }
                else
                {
                    List<string> servicios = new List<string>();
                    foreach (var servicio in Servicios)
                    {
                        servicios.Add(servicio.Value<string>("name"));
                    }
                    var options = Servicios.Select(documentType => new
                    {
                        Id = documentType["id"].ToString(),
                        Name = documentType["shortName"].ToString()
                    }).ToArray(); var optionsList = options.ToList();
                    var newOption = new
                    {
                        Id = "Atras",
                        Name = "Atras"
                    };

                    optionsList.Add(newOption);
                    var promptOptions = new PromptOptions
                    {
                        Prompt = MessageFactory.Text("Selecciona un servicio:"),
                        Choices = ChoiceFactory.ToChoices(options.Select(option => option.Name).ToList()),
                    };
                    stepContext.Values["Services"] = options;
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

        private async Task<DialogTurnResult> HandleServiceSelectionAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var options = (IEnumerable<dynamic>)stepContext.Values["Services"];

            var choice = (FoundChoice)stepContext.Result;
            var selectedOption = options.FirstOrDefault(option => option.Name == choice.Value);
            var userProfile = await _userStateAccessor.GetAsync(stepContext.Context, () => new UserProfileModel(), cancellationToken);
            if (userProfile.Servicios != null)
            {
                return await stepContext.NextAsync();
            };

            if (selectedOption != null)
            {
                if (choice.Value.Equals("Atras", StringComparison.OrdinalIgnoreCase))
                {
                    userProfile.Servicios = null;
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
            var userProfile = await _userStateAccessor.GetAsync(stepContext.Context, () => new UserProfileModel(), cancellationToken);
            if (userProfile.especialidad != null)
            {
                return await stepContext.NextAsync();
            };
            try
            {
                List<JObject> especialidades = await _apiCalls.GetEspecialidadAsync(userProfile.Servicios);
                if (especialidades == null || !especialidades.Any())
                {
                    await stepContext.Context.SendActivityAsync("No se encontraron especialidades disponibles.", cancellationToken: cancellationToken);
                    return await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), null, cancellationToken);
                }
                else
                {
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
                        Prompt = MessageFactory.Text("Selecciona un servicio:"),
                        Choices = ChoiceFactory.ToChoices(options.Select(option => option.Name).ToList()),
                        Style = ListStyle.List
                    };
                    stepContext.Values["Especialidades"] = options;
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

        private async Task<DialogTurnResult> GetScheduleAvailabilityAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var optionsAnt = (IEnumerable<dynamic>)stepContext.Values["Especialidades"];
            var choice = (FoundChoice)stepContext.Result;
            var userProfile = await _userStateAccessor.GetAsync(stepContext.Context, () => new UserProfileModel(), cancellationToken);
            if (choice.Value.Equals("Atras", StringComparison.OrdinalIgnoreCase))
            {
                userProfile.especialidad = null;
                await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                return await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), null, cancellationToken);
            }
            var selectedOption = optionsAnt.FirstOrDefault(option => option.Name == choice.Value);
            var dialogState = await _dialogStateAccessor.GetAsync(stepContext.Context, () => new DialogState(), cancellationToken);
            stepContext.Values["DialogState"] = dialogState;
            userProfile.especialidad = selectedOption.Id;
            try
            {
                List<JObject> ScheduleAvailability = await _apiCalls.GetScheduleAvailabilityAsync(userProfile);
                if (ScheduleAvailability == null || !ScheduleAvailability.Any())
                {
                    await stepContext.Context.SendActivityAsync("No se encontraron especialidades disponibles.", cancellationToken: cancellationToken);
                    return await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), null, cancellationToken);
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
                    var choices = options.Select(option => new Choice
                    {
                        Value = $"{option.DateTime}{Environment.NewLine}Doctor: {option.DoctorName}{Environment.NewLine}Duración: {option.Duration}"
                    }).ToList();
                    var promptOptions = new PromptOptions
                    {
                        Prompt = MessageFactory.Text("Selecciona una fecha para tu cita: "+ Environment.NewLine),
                        Choices = ChoiceFactory.ToChoices(options.Select(option => option.DateTime).ToList()),
                        Style = ListStyle.List
                    };

                    stepContext.Values["ScheduleAvailability"] = options;
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

        private async Task<DialogTurnResult> HandleScheduleAvailabilityAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var options = (IEnumerable<dynamic>)stepContext.Values["ScheduleAvailability"];

            var choice = (FoundChoice)stepContext.Result;
            var selectedOption = options.FirstOrDefault(option => option.DateTime == choice.Value);

            if (selectedOption != null)
            {
                if (choice.Value.Equals("Atras", StringComparison.OrdinalIgnoreCase))
                {
                    return await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), null, cancellationToken);
                }
                var userProfile = await _userStateAccessor.GetAsync(stepContext.Context, () => new UserProfileModel(), cancellationToken);
                stepContext.Values["DataCita"] = selectedOption;

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
                        new Choice { Value = "Sí" },
                        new Choice { Value = "No" }
                    },
                    Style = ListStyle.List
                };
                var option = stepContext.Values["DataCita"];
                await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                return await stepContext.PromptAsync(nameof(ChoicePrompt), promptOptions, cancellationToken);

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

            if (selectedOption != null)
            {
                if (choice.Value.Equals("Atras", StringComparison.OrdinalIgnoreCase))
                {
                    return await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), null, cancellationToken);
                }
                if (choice.Value.Equals("Si", StringComparison.OrdinalIgnoreCase))
                {
                    await stepContext.Context.SendActivityAsync("Agendando cita...");
                    //await _apiCalls.PostCreateCitaAsync(userProfile, stepContext.Values["DataCita"]);
                    await Task.Delay(3000);
                    await stepContext.Context.SendActivityAsync("Cita agendada correctamente.");
                    await stepContext.Context.SendActivityAsync("¿Te podemos ayudar en algo mas?");
                    return await stepContext.NextAsync();

                }
                stepContext.Values["DataCita"] = selectedOption;

                return await stepContext.NextAsync();
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
            // Validación exitosa
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
            if (string.IsNullOrEmpty(userProfile.DocumentId))
            {
                userProfile.DocumentId = data;
                message = "";
            }
            else
            {
                message = "";
            }
            await _userState.SaveChangesAsync(Context.Context, false, cancellationToken);
            return message; ;
        }
    }
}
