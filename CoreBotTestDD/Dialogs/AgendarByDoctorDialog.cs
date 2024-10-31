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
using System.Xml.Linq;

namespace DonBot.Dialogs
{
    public class AgendarByDoctorDialog : ComponentDialog
    {
        private readonly UserState _userState;
        private readonly ApiCalls _apiCalls;
        private readonly ConversationState _conversationState;
        private readonly ILogger _logger;
        private readonly IStatePropertyAccessor<DialogState> _dialogStateAccessor;
        private readonly IStatePropertyAccessor<UserProfileModel> _userStateAccessor;

        public AgendarByDoctorDialog(ILogger<MainDialog> logger, UserState userState, ConversationState conversationState, ApiCalls apiCalls, CustomChoicePrompt customChoicePrompt)
           : base(nameof(AgendarByDoctorDialog))
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
                GetDoctorNameAsync,
                GetDoctorAsync,
                HandleDoctorSelectionAsync,
                HandleServicesByDoctorAsync,
                HandleServiceSelectionByDoctorAsync,
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
        private async Task<DialogTurnResult> GetDoctorNameAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userProfile = await _userStateAccessor.GetAsync(stepContext.Context, () => new UserProfileModel(), cancellationToken);
            if (userProfile.DoctorName != null)
            {
                return await stepContext.NextAsync();
            }
            userProfile.SaveData = true;
            userProfile.CurrentPage = 0;
            var choice = (FoundChoice)stepContext.Result;
            if (choice != null && choice.Value.Equals("Atras", StringComparison.OrdinalIgnoreCase))
            {
                stepContext.Context.Activity.Text = null;
                var cancellationReason = new { Reason = DialogReason.ReplaceCalled };
                await stepContext.CancelAllDialogsAsync(cancellationToken);
                return await stepContext.EndDialogAsync(cancellationReason, cancellationToken);
            }
            await stepContext.Context.SendActivityAsync("¿Con cual doctor deseas agendar tu cita? \n\n Escribe solamente el apellido del doctor, no la especialidad ni la palabra doctor.\n\n Ejemplo: López, Rodriguez, Gomez.");
            userProfile.SaveData = true;
            await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
            return new DialogTurnResult(DialogTurnStatus.Waiting);
        }

        private async Task<DialogTurnResult> GetDoctorAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {var dialogState = await _dialogStateAccessor.GetAsync(stepContext.Context, () => new DialogState(), cancellationToken);
            stepContext.Values["DialogState"] = dialogState;
            var userProfile = await _userStateAccessor.GetAsync(stepContext.Context, () => new UserProfileModel(), cancellationToken);
            userProfile.SaveData = false;
            if (stepContext.Context.Activity.Text != null && stepContext.Context.Activity.Text.ToString().Equals("Atras", StringComparison.OrdinalIgnoreCase))
            {
                stepContext.Context.Activity.Text = null;
                var cancellationReason = new { Reason = DialogReason.ReplaceCalled };
                await stepContext.CancelAllDialogsAsync(cancellationToken);
                return await stepContext.EndDialogAsync(cancellationReason, cancellationToken);
            }
            if (userProfile.DoctorId != null)
            {
                return await stepContext.NextAsync();
            };
            try
            {
                List<JObject> Doctores;
                Doctores = await _apiCalls.GetDoctorListAsync(userProfile.CodeCompany, userProfile.DoctorName);
                if (Doctores == null || !Doctores.Any())
                {
                    userProfile.DoctorName = null;
                    await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                    await stepContext.Context.SendActivityAsync("No se encontraron doctores.", cancellationToken: cancellationToken);
                    return await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), null, cancellationToken);
                }
                else
                {
                    int currentPage = userProfile.CurrentPage;
                    int itemsPerPage = 6;
                    var paginatedDocumentTypes = Doctores.Skip(currentPage * itemsPerPage).Take(itemsPerPage).ToList();
                    List<string> servicios = new List<string>();
                    foreach (var servicio in Doctores)
                    {
                        servicios.Add(servicio.Value<string>("name"));
                    }
                    var options = paginatedDocumentTypes.Select(documentType => new
                    {
                        Id = documentType["id"].ToString(),
                        Name = $"{documentType["name"]} {documentType["lastName"]}",
                    }).ToArray();
                    var optionsList = options.ToList();
                    if (Doctores.Count > (currentPage + 1) * itemsPerPage)
                    {
                        var nextPageOption = new { Id = "SiguientePágina", Name = "Siguiente Página"};
                        optionsList.Add(nextPageOption);
                    }
                    if (currentPage > 0)
                    {
                        var previousPageOption = new { Id = "PaginaAnterior", Name = "Página Anterior"};
                        optionsList.Add(previousPageOption);
                    }
                    var backOption = new { Id = "Atras", Name = "Atras"};
                    optionsList.Add(backOption);

                    var promptOptions = new PromptOptions
                    {
                        Prompt = MessageFactory.Text("Selecciona un doctor:"),
                        Choices = ChoiceFactory.ToChoices(optionsList.Select(option => option.Name).ToList()),
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
            var optionsDoctor = (IEnumerable<dynamic>)stepContext.Values["Doctores"];
            var selectedOption = optionsDoctor.FirstOrDefault(option => option.Name == choice.Value);
            if (selectedOption != null)
            {
                if (choice != null && choice.Value.Equals("Atras", StringComparison.OrdinalIgnoreCase))
                {
                    userProfile.DoctorName = null;
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
                else if (choice.Value.Equals("Página Anterior", StringComparison.OrdinalIgnoreCase))
                {
                    userProfile.CurrentPage--;
                    await _userStateAccessor.SetAsync(stepContext.Context, userProfile, cancellationToken);
                    await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                    return await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), null, cancellationToken);
                }
                userProfile.DoctorId = selectedOption.Id;
                userProfile.CurrentPage = 0;
                return await stepContext.NextAsync();
            }
            else
            {
                await stepContext.Context.SendActivityAsync("Opción seleccionada no válida.", cancellationToken: cancellationToken);
                return await stepContext.EndDialogAsync();
            }
        }

        private async Task<DialogTurnResult> HandleServicesByDoctorAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userProfile = await _userStateAccessor.GetAsync(stepContext.Context, () => new UserProfileModel(), cancellationToken);
            if(userProfile.Servicios != null)
            {
                return await stepContext.NextAsync();
            }
            try
            {
                List<JObject> Servicios = await _apiCalls.GetServicesByDoctorAsync(userProfile.DoctorId, userProfile.CodeCompany, userProfile.PlanAseguradora);
                if (Servicios == null || !Servicios.Any())
                {
                    await stepContext.Context.SendActivityAsync("Actualmente el profesional no cuenta con servicios para auto agendamiento, te invitamos a comunicarte con los numeros de atencion de la institucion.", cancellationToken: cancellationToken);
                    var choices = new List<CustomChoice>
                    {
                     new CustomChoice { Value = "Si", Name = "Si" },
                        new CustomChoice { Value = "No", Name = "No" },
                        new CustomChoice { Value = "Atras", Name = "Atras" }
                    };
                    var promptOptions = new PromptOptions
                    {
                        Prompt = MessageFactory.Text("¿Deseas realizar la consulta con otro profesional?"),
                        Choices = choices.Cast<Choice>().ToList(),
                        Style = ListStyle.List
                    };
                    stepContext.Values["Servicios"] = choices;
                    await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                    return await stepContext.PromptAsync(nameof(CustomChoicePrompt), promptOptions, cancellationToken);
                }
                else
                {
                    int currentPage = userProfile.CurrentPage;
                    int itemsPerPage = 6;
                    var paginatedDocumentTypes = Servicios.Skip(currentPage * itemsPerPage).Take(itemsPerPage).ToList();
                    List<string> servicios = new List<string>();
                    foreach (var servicio in Servicios)
                    {
                        servicios.Add(servicio.Value<string>("name"));
                    }
                    var options = paginatedDocumentTypes.Select(documentType => new
                    {
                        Id = documentType["id"].ToString(),
                        Name = documentType["name"].ToString(),
                        Selected = documentType["selected"].ToString()
                    }).ToArray();
                    var optionsList = options.ToList();
                    if (Servicios.Count > (currentPage + 1) * itemsPerPage)
                    {
                        var nextPageOption = new { Id = "SiguientePágina", Name = "Siguiente Página", Selected = "Siguiente Página"};
                        optionsList.Add(nextPageOption);
                    }
                    if (currentPage > 0)
                    {
                        var previousPageOption = new { Id = "PaginaAnterior", Name = "Página Anterior", Selected = "Página Anterior" };
                        optionsList.Add(previousPageOption);
                    }
                    var backOption = new { Id = "Atras", Name = "Atras", Selected = "Atras" };
                    optionsList.Add(backOption);
                    var promptOptions = new PromptOptions
                    {
                        Prompt = MessageFactory.Text("Selecciona el servicio que deseas:"),
                        Choices = ChoiceFactory.ToChoices(optionsList.Select(option => option.Name).ToList()),
                        Style = ListStyle.List
                    };
                    stepContext.Values["Servicios"] = optionsList;
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

        private async Task<DialogTurnResult> HandleServiceSelectionByDoctorAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            var choice = (FoundChoice)stepContext.Result;
            var userProfile = await _userStateAccessor.GetAsync(stepContext.Context, () => new UserProfileModel(), cancellationToken);
            if (userProfile.Servicios != null)
            {
                return await stepContext.NextAsync();
            }
            var options = (IEnumerable<dynamic>)stepContext.Values["Servicios"];
            var selectedOption = options.FirstOrDefault(option => option.Name == choice.Value);
            if (selectedOption != null)
            {
                if (choice != null && choice.Value.Equals("Atras", StringComparison.OrdinalIgnoreCase))
                {
                    userProfile.DoctorId = null;
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
                else if (choice.Value.Equals("Página Anterior", StringComparison.OrdinalIgnoreCase))
                {
                    userProfile.CurrentPage--;
                    await _userStateAccessor.SetAsync(stepContext.Context, userProfile, cancellationToken);
                    await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                    return await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), null, cancellationToken);
                }
                else if (choice.Value.Equals("No", StringComparison.OrdinalIgnoreCase))
                {
                    await stepContext.Context.SendActivityAsync("¿Puedo ayudarte en algo mas🤓?", cancellationToken: cancellationToken);
                    var cancellationReason = new { Reason = DialogReason.CancelCalled };
                    await stepContext.CancelAllDialogsAsync(cancellationToken);
                    return await stepContext.EndDialogAsync(cancellationReason, cancellationToken);
                }
                else if (choice.Value.Equals("Si", StringComparison.OrdinalIgnoreCase))
                {
                    userProfile.Servicios = null;
                    userProfile.DoctorId = null;
                    userProfile.DoctorName = null;
                    stepContext.Context.Activity.Text = null;
                    var cancellationReason = new { Reason = DialogReason.ReplaceCalled };
                    await stepContext.CancelAllDialogsAsync(cancellationToken);
                    return await stepContext.EndDialogAsync(cancellationReason, cancellationToken);
                }
                else if (selectedOption.Selected == "False")
                {
                    await stepContext.Context.SendActivityAsync("Este servicio no se puede agendar por este medio, puede dirigirse a la pagina [URL](http://fundacionhomi.org/.co/) para realizar el agendamiento desde alli.", cancellationToken: cancellationToken);
                    stepContext.Context.Activity.Text = null;
                    userProfile.DoctorId = null;
                    userProfile.DoctorName = null;
                    await _conversationState.SaveChangesAsync(stepContext.Context);
                    await stepContext.Context.SendActivityAsync("¿Puedo ayudarte en algo mas🤓?", cancellationToken: cancellationToken);
                    await ResetUserProfile(stepContext);
                    var cancellationReason = new { Reason = DialogReason.CancelCalled };
                    await stepContext.CancelAllDialogsAsync(cancellationToken);
                    return await stepContext.EndDialogAsync(cancellationReason, cancellationToken);
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
                List<JObject> especialidades = await _apiCalls.GetEspecialidadAsync(userProfile.Servicios, userProfile.CodeCompany);
                if (especialidades == null || !especialidades.Any())
                {
                    await stepContext.Context.SendActivityAsync("No se encontraron especialidades disponibles.", cancellationToken: cancellationToken);
                    userProfile.Servicios = null;
                    return await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), null, cancellationToken);
                }
                else
                {
                    if (especialidades.Count == 1)
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
            if (userProfile.especialidad == null)
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
                Prompt = MessageFactory.Text("Para continuar con el agendamiento de tu cita, por favor selecciona una de las siguientes opciones: "),
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
            if (userProfile.CitaDate != null)
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
                    var cancellationReasonReturn = new { Reason = DialogReason.CancelCalled };
                    await stepContext.CancelAllDialogsAsync(cancellationToken);
                    return await stepContext.EndDialogAsync(cancellationReasonReturn, cancellationToken);
                }
                else if (cancellationReason.Reason != null && cancellationReason.Reason == DialogReason.ReplaceCalled)
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
            if (userProfile.CitaDate != null)
            {
                return await stepContext.NextAsync();
            }
            var choice = (FoundChoice)stepContext.Result;
            if (choice != null && choice.Value.Equals("Atras", StringComparison.OrdinalIgnoreCase))
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
                userProfile.CitaDate = selectedOption.DateTime;
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
            ScheduleAvailability = await _apiCalls.GetScheduleAvailabilitySpecificAsync(userProfile);
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
                userProfile.CitaDate = null;
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
                string citaInf = string.Concat(userProfile.CitaDate + " - " + stepContext.Values["DataCitaHour"].GetType().GetProperty("DateTime").GetValue(stepContext.Values["DataCitaHour"]));
                var promptOptions = new PromptOptions
                {
                    Prompt = MessageFactory.Text("Confirmamos tu cita para el día: " + citaInf),
                    Choices = new List<Choice>
                    {
                        new Choice { Value = "Si" },
                        new Choice { Value = "No" }
                    },
                    Style = ListStyle.List
                };
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
                string result = await _apiCalls.PostCreateCitaAsync(userProfile, stepContext.Values["DataCitaHour"]);
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
                    await ResetUserProfile(stepContext);
                    await stepContext.Context.SendActivityAsync("¿Te podemos ayudar en algo mas?");
                    var cancellationReason = new { Reason = DialogReason.CancelCalled };
                    await stepContext.CancelAllDialogsAsync(cancellationToken);
                    return await stepContext.EndDialogAsync(cancellationReason, cancellationToken);
                }
                else if (result.Equals("limited"))
                {
                    await ResetUserProfile(stepContext);
                    await stepContext.Context.SendActivityAsync("No hay cupos disponibles para el Mes, aseguradora, plan y la fecha seleccionada, no es posible crear la cita.");
                    return await stepContext.EndDialogAsync();
                }
                else
                {
                    await ResetUserProfile(stepContext);
                    await stepContext.Context.SendActivityAsync("Error al generar la cita. Intenta mas tarde.");
                    var cancellationReason = new { Reason = DialogReason.CancelCalled };
                    await stepContext.CancelAllDialogsAsync(cancellationToken);
                    return await stepContext.EndDialogAsync(cancellationReason, cancellationToken);

                }
            }
            else if (choice.Value.Equals("No", StringComparison.OrdinalIgnoreCase))
            {
                await _userState.ClearStateAsync(stepContext.Context);
                stepContext.Values.Clear();
                await ResetUserProfile(stepContext);
                await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                await stepContext.Context.SendActivityAsync("Agendamiento cancelado. ¿Te podemos ayudar en algo mas?");
                var cancellationReason = new { Reason = DialogReason.CancelCalled };
                await stepContext.CancelAllDialogsAsync(cancellationToken);
                return await stepContext.EndDialogAsync(cancellationReason, cancellationToken);
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
                userProfile.DoctorName = innerDc.Context.Activity.Text.ToString();
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
            userProfile.DoctorId = null;
            userProfile.DoctorName = null;
            userProfile.TypeConsult = null;
            userProfile.CurrentPage = 0;

            await _userStateAccessor.SetAsync(context.Context, userProfile, cancellationToken);
            await _userState.SaveChangesAsync(context.Context, false, cancellationToken);
        }
    }
    public class CustomChoice : Choice
    {
        public string Name { get; set; }
    }
}
