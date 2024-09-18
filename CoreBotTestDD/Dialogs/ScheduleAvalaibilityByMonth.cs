using CoreBotTestDD.Dialogs;
using CoreBotTestDD.Models;
using CoreBotTestDD.Services;
using DonBot.Utilities;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace DonBot.Dialogs
{
    public class ScheduleAvalaibilityByMonth : ComponentDialog
    {
        private readonly UserState _userState;
        private readonly ApiCalls _apiCalls;
        private readonly CLUService _cluService;
        private readonly DataValidation _dataValidation;
        private readonly ConversationState _conversationState;
        private readonly ILogger _logger;
        private readonly IStatePropertyAccessor<DialogState> _dialogStateAccessor;
        private readonly IStatePropertyAccessor<UserProfileModel> _userStateAccessor;
        public ScheduleAvalaibilityByMonth(ILogger<MainDialog> logger, CLUService cluService, UserState userState, ConversationState conversationState, ApiCalls apiCalls, CustomChoicePrompt customChoicePrompt)
           : base(nameof(ScheduleAvalaibilityByMonth))
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
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> HandleScheduleAvailabilityByMonthAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userProfile = await _userStateAccessor.GetAsync(stepContext.Context, () => new UserProfileModel(), cancellationToken);
            if (userProfile.AvailabilityChoice != "Month")
            {
                return await stepContext.NextAsync();
            }
            var choices = new List<Choice>
             {
                 new Choice { Value = "Enero" },
                 new Choice { Value = "Febrero" },
                 new Choice { Value = "Marzo" },
                 new Choice { Value = "Abril" },
                 new Choice { Value = "Mayo" },
                 new Choice { Value = "Junio" },
                 new Choice { Value = "Julio" },
                 new Choice { Value = "Agosto" },
                 new Choice { Value = "Septiembre" },
                 new Choice { Value = "Octubre" },
                 new Choice { Value = "Noviembre" },
                 new Choice { Value = "Dociembre" },
                 new Choice { Value = "Atras" },
             };
            var promptOptions = new PromptOptions
            {
                Prompt = MessageFactory.Text("Entendido por favor indícame el número de mes."),
                Choices = choices,
                Style = ListStyle.List
            };
            stepContext.Values["Choice"] = choices;
            await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
            return await stepContext.PromptAsync(nameof(CustomChoicePrompt), promptOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> HandleScheduleAvalibilityByMonthConsultAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userProfile = await _userStateAccessor.GetAsync(stepContext.Context, () => new UserProfileModel(), cancellationToken);
            if (userProfile.AvailabilityChoice != "Month")
            {
                return await stepContext.NextAsync();
            }
            var choice = (FoundChoice)stepContext.Result;
            if (choice != null)
            {
                if (choice.Value.Equals("Atras", StringComparison.OrdinalIgnoreCase))
                {
                    userProfile.AvailabilityChoice = null;
                    await _conversationState.SaveChangesAsync(stepContext.Context);
                    await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                    return await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), null, cancellationToken);
                }
                else
                {
                    List<JObject> ScheduleAvailability = await _apiCalls.GetScheduleAvailabilityByMonthAsync(userProfile, choice.Index);
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

    }
}
