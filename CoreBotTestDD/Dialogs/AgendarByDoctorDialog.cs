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

namespace DonBot.Dialogs
{
    public class AgendarByDoctorDialog : ComponentDialog
    {
        private readonly UserState _userState;
        private readonly ApiCalls _apiCalls;
        private readonly CLUService _cluService;
        private readonly ConversationState _conversationState;
        private readonly RegisterDialog _registerDialog;
        private readonly ILogger _logger;
        private readonly IStatePropertyAccessor<DialogState> _dialogStateAccessor;
        private readonly IStatePropertyAccessor<UserProfileModel> _userStateAccessor;


        public AgendarByDoctorDialog(ILogger<MainDialog> logger, CLUService cluService, UserState userState, ConversationState conversationState, ApiCalls apiCalls, CustomChoicePrompt customChoicePrompt)
               : base(nameof(AgendarByDoctorDialog))
        {
            _userState = userState ?? throw new ArgumentNullException(nameof(userState));
            _conversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));
            _logger = logger;
            _apiCalls = apiCalls;
            _cluService = cluService;
            _dialogStateAccessor = _userState.CreateProperty<DialogState>("DialogState");
            _userStateAccessor = _userState.CreateProperty<UserProfileModel>("UserProfile");

            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt), ChoiceValidatorAsync));
            AddDialog(customChoicePrompt);

            var waterfallSteps = new WaterfallStep[]
            {
            };
        }

        private async Task<DialogTurnResult> GetDoctorNameAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
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
            if (userProfile.DoctorId != null)
            {
                return await stepContext.NextAsync();
            }
            await stepContext.Context.SendActivityAsync("¿Con cual doctor deseas agendar tu cita? \n\n Escribe solamente el apellido del doctor, no la especialidad ni la palabra doctor.\n\n Ejemplo: López, Rodriguez, Gomez.");
            userProfile.SaveData = false;
            await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
            return new DialogTurnResult(DialogTurnStatus.Waiting);
        }

        private async Task<DialogTurnResult> GetDoctorAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var dialogState = await _dialogStateAccessor.GetAsync(stepContext.Context, () => new DialogState(), cancellationToken);
            stepContext.Values["DialogState"] = dialogState;
            var userProfile = await _userStateAccessor.GetAsync(stepContext.Context, () => new UserProfileModel(), cancellationToken);
            if (stepContext.Context.Activity.Text != null && stepContext.Context.Activity.Text.ToString().Equals("Atras", StringComparison.OrdinalIgnoreCase))
            {
                await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                return await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), null, cancellationToken);
            }
            if (userProfile.DoctorId != null)
            {
                return await stepContext.NextAsync();
            };
            try
            {
                List<JObject> Doctores;
                Doctores = await _apiCalls.GetDoctorListAsync(userProfile.CodeCompany, stepContext.Context.Activity.Text.ToString());
                if (Doctores == null || !Doctores.Any())
                {
                    await stepContext.Context.SendActivityAsync("No se encontraron servicios disponibles.", cancellationToken: cancellationToken);
                    return await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), null, cancellationToken);
                }
                else
                {
                    List<string> servicios = new List<string>();
                    foreach (var servicio in Doctores)
                    {
                        servicios.Add(servicio.Value<string>("name"));
                    }
                    var options = Doctores.Select(documentType => new
                    {
                        Id = documentType["id"].ToString(),
                        Name = documentType["shortName"].ToString(),
                        LastName = documentType["lastName"].ToString()
                    }).ToArray(); var optionsList = options.ToList();
                    var newOption = new
                    {
                        Id = "Atras",
                        Name = "Atras",
                        LastName = "Atras"
                    };

                    optionsList.Add(newOption);
                    var promptOptions = new PromptOptions
                    {
                        Prompt = MessageFactory.Text("Selecciona un servicio:"),
                        Choices = ChoiceFactory.ToChoices(optionsList.Select(option => $"{option.Name} {option.LastName}").ToList()),
                        Style = ListStyle.List
                    };
                    stepContext.Values["Doctores"] = optionsList;
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

        private async Task<DialogTurnResult> HandleDoctorSelectionAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            var choice = (FoundChoice)stepContext.Result;
            var userProfile = await _userStateAccessor.GetAsync(stepContext.Context, () => new UserProfileModel(), cancellationToken);
            if (userProfile.DoctorId != null)
            {
                return await stepContext.NextAsync();
            }
            var options = (IEnumerable<dynamic>)stepContext.Values["Doctores"];
            var selectedOption = options.FirstOrDefault(option => option.Name == choice.Value);
            if (selectedOption != null)
            {
                if (choice != null && choice.Value.Equals("Atras", StringComparison.OrdinalIgnoreCase))
                {
                    stepContext.Context.Activity.Text = null;
                    await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                    return await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), null, cancellationToken);
                }
                userProfile.DoctorId = selectedOption.Id;
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

        private async Task<DialogTurnResult> GetServiceNameAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userProfile = await _userStateAccessor.GetAsync(stepContext.Context, () => new UserProfileModel(), cancellationToken);
            if (userProfile.Servicios != null)
            {
                return await stepContext.NextAsync();
            }
            await stepContext.Context.SendActivityAsync("Que servicio deseas agendar? (Ejemplo: Cardiologia) \n\n Escribe atras si quieres cambiar tu informacion anterior.");
            userProfile.SaveData = false;
            await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
            return new DialogTurnResult(DialogTurnStatus.Waiting);
        }

        private Task<bool> ChoiceValidatorAsync(PromptValidatorContext<FoundChoice> promptContext, CancellationToken cancellationToken)
        {
            var selectedOption = promptContext.Recognized.Value;
            if (selectedOption == null || string.IsNullOrWhiteSpace(selectedOption.Value))
            {
                promptContext.Context.SendActivityAsync("Por favor, selecciona una opción válida.", cancellationToken: cancellationToken);
                return Task.FromResult(false);
            }
            return Task.FromResult(true);
        }
    
    
    
    
    
    }
    }
