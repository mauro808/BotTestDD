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
using System.Xml.Linq;

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
                HandleScheduleAvailabilityByMonthAsync,
                HandleScheduleAvalibilityByMonthConsultAsync,
                HandleScheduleAvailabilityAsync,
                GetConfirmationAsync,
                HandleConfirmationCitaAsync
            };
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> HandleScheduleAvailabilityByMonthAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userProfile = await _userStateAccessor.GetAsync(stepContext.Context, () => new UserProfileModel(), cancellationToken);
            if (userProfile.AvailabilityChoiceMonth != 0)
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
            userProfile.CurrentPage = 0;
            stepContext.Values["Choice"] = choices;
            await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
            return await stepContext.PromptAsync(nameof(CustomChoicePrompt), promptOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> HandleScheduleAvalibilityByMonthConsultAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userProfile = await _userStateAccessor.GetAsync(stepContext.Context, () => new UserProfileModel(), cancellationToken);
            var choice = (FoundChoice)stepContext.Result;
            if (choice != null || userProfile.AvailabilityChoiceMonth != 0)
            {
                if (choice != null && choice.Value.Equals("Atras", StringComparison.OrdinalIgnoreCase))
                {
                    var cancellationReason = new { Reason = DialogReason.ReplaceCalled };
                    await stepContext.CancelAllDialogsAsync(cancellationToken);
                    return await stepContext.EndDialogAsync(cancellationReason, cancellationToken);
                }
                else
                {
                    List<JObject> ScheduleAvailability;
                    if (choice != null)
                    {
                        ScheduleAvailability = await _apiCalls.GetScheduleAvailabilityByMonthAsync(userProfile, choice.Index + 1);
                        userProfile.AvailabilityChoiceMonth = choice.Index + 1;
                    }
                    else
                    {
                        ScheduleAvailability = await _apiCalls.GetScheduleAvailabilityByMonthAsync(userProfile, userProfile.AvailabilityChoiceMonth);
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

                        stepContext.Values["ScheduleAvailability"] = optionsList;
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

        private async Task<DialogTurnResult> HandleScheduleAvailabilityAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userProfile = await _userStateAccessor.GetAsync(stepContext.Context, () => new UserProfileModel(), cancellationToken);
            var options = (IEnumerable<dynamic>)stepContext.Values["ScheduleAvailability"];
            var choice = (FoundChoice)stepContext.Result;
            var selectedOption = options.FirstOrDefault(option => option.DateTime == choice.Value);
            if (selectedOption != null)
            {
                if (choice.Value.Equals("Atras", StringComparison.OrdinalIgnoreCase))
                {
                    stepContext.Context.Activity.Text = null;
                    userProfile.AvailabilityChoiceMonth = 0;
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
                stepContext.Values["DataCita"] = selectedOption;
                userProfile.AvailabilityChoice = choice.Value;
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
                return await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), null, cancellationToken);
            }
            if (choice.Value.Equals("Si", StringComparison.OrdinalIgnoreCase))
            {
                await stepContext.Context.SendActivityAsync("Agendando cita...");
                bool result = await _apiCalls.PostCreateCitaAsync(userProfile, stepContext.Values["DataCita"], stepContext.Values["DataCita"]);
                await Task.Delay(3000);
                if (result == true)
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
                    await stepContext.Context.SendActivityAsync("Informacion de tu cita:\n\n Fecha: " + (string)stepContext.Values["DataCita"].GetType().GetProperty("DateTime").GetValue(stepContext.Values["DataCita"]) + "\n\n Doctor: " + (string)stepContext.Values["DataCita"].GetType().GetProperty("DoctorName").GetValue(stepContext.Values["DataCita"]));
                    await ResetUserProfile(stepContext);
                    await stepContext.Context.SendActivityAsync("¿Te podemos ayudar en algo mas?");
                    var cancellationReason = new { Reason = DialogReason.ReplaceCalled };
                    await stepContext.CancelAllDialogsAsync(cancellationToken);
                    return await stepContext.EndDialogAsync(cancellationReason, cancellationToken);
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

        private async Task ResetUserProfile(DialogContext context, CancellationToken cancellationToken = default)
        {
            var userProfile = await _userStateAccessor.GetAsync(context.Context, () => new UserProfileModel(), cancellationToken);

            userProfile.AvailabilityChoiceMonth = 0;
            userProfile.CurrentPage = 0;

            await _userStateAccessor.SetAsync(context.Context, userProfile, cancellationToken);
            await _userState.SaveChangesAsync(context.Context, false, cancellationToken);
        }

    }
}
