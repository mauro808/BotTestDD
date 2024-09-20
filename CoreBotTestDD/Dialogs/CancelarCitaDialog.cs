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
    public class CancelarCitaDialog : ComponentDialog
    {
        private readonly UserState _userState;
        private readonly ApiCalls _apiCalls;
        private readonly ConversationState _conversationState;
        private readonly ILogger _logger;
        private readonly IStatePropertyAccessor<DialogState> _dialogStateAccessor;
        private readonly IStatePropertyAccessor<UserProfileModel> _userStateAccessor;

        public CancelarCitaDialog(ILogger<MainDialog> logger, UserState userState, ConversationState conversationState, ApiCalls apiCalls, CustomChoicePrompt customChoicePrompt, RegisterDialog registerDialog)
          : base(nameof(CancelarCitaDialog))
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
                HandleCitaSelectionAsync,
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
                    userProfile.IsNewUser = false;
                    await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                    stepContext.Context.Activity.Text = null;
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
            await stepContext.Context.SendActivityAsync("Escribe el número de tu documento ❔ sin puntos ni comas. (Ejemplo: 1001122345) Escribe atrás si quieres cambiar tu información anterior.");
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
                    await stepContext.Context.SendActivityAsync("¿Te puedo ayudar en algo mas?.", cancellationToken: cancellationToken);
                    return await stepContext.EndDialogAsync();
                }
                else{
                    var options = citas.Select(documentType => new
                    {
                        Id = documentType["appointmentID"].ToString(),
                        Name = documentType["appointmentTypeName"].ToString(),
                        Date = documentType["dateFrom"].ToString()
                    }).ToArray(); var optionsList = options.ToList();
                    var newOption = new
                    {
                        Id = "Atras",
                        Name = "Atras",
                        Date = "Atras"
                    };
                    optionsList.Add(newOption);
                    var promptOptions = new PromptOptions
                    {
                        Prompt = MessageFactory.Text("Selecciona la cita a cancelar:"),
                        Choices = ChoiceFactory.ToChoices(optionsList.Select(option => option.Name).ToList()),
                        Style = ListStyle.List
                    };
                    stepContext.Values["Citas"] = optionsList;
                    await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                    return await stepContext.PromptAsync(nameof(CustomChoicePrompt), promptOptions, cancellationToken);
                }
            }
            catch(Exception e)
            {
                await stepContext.Context.SendActivityAsync("Error en la consulta");
                return await stepContext.EndDialogAsync();
            }
        }

        private async Task<DialogTurnResult> HandleCitaSelectionAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userProfile = await _userStateAccessor.GetAsync(stepContext.Context, () => new UserProfileModel(), cancellationToken);
            var options = (IEnumerable<dynamic>)stepContext.Values["Citas"];
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
                var promptOptions = new PromptOptions
                {
                    Prompt = MessageFactory.Text("Confirmas la cancelacion de tu cita para el dia: " + selectedOption.Date + " Asignada a: " + selectedOption.Name),
                    Choices = new List<Choice>
                    {
                        new Choice { Value = "Si" },
                        new Choice { Value = "No" }
                    },
                    Style = ListStyle.List
                };
                stepContext.Values["Cita"] = selectedOption;
                stepContext.Values["Choices"] = options;
                await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                return await stepContext.PromptAsync(nameof(CustomChoicePrompt), promptOptions, cancellationToken);
            }
            else
            {
                await stepContext.Context.SendActivityAsync("Opción seleccionada no válida.", cancellationToken: cancellationToken);
                return await stepContext.EndDialogAsync();
            }
        }

        private async Task<DialogTurnResult> HandleConfirmationCitaAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var cita = (dynamic)stepContext.Values["Cita"];
            var userProfile = await _userStateAccessor.GetAsync(stepContext.Context, () => new UserProfileModel(), cancellationToken);
            var choice = (FoundChoice)stepContext.Result;
            if (choice != null && choice.Value.Equals("Atras", StringComparison.OrdinalIgnoreCase))
            {
                return await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), null, cancellationToken);
            }
            if (choice.Value.Equals("Si", StringComparison.OrdinalIgnoreCase))
            {
                await stepContext.Context.SendActivityAsync("Cancelando cita...");
                bool result = await _apiCalls.PostCancelCitaAsync(cita.Id, userProfile.UserId, userProfile.CodeCompany);
                await Task.Delay(3000);
                if (result == true)
                {
                    await stepContext.Context.SendActivityAsync("Cita cancelada correctamente. ✅");
                    List<string> names = await _apiCalls.GetPreparationAsync(userProfile);
                    if (names != null)
                    {
                        foreach (string name in names)
                        {
                            await stepContext.Context.SendActivityAsync(name);
                        }
                    }
                    await ResetUserProfile(stepContext);
                    await stepContext.Context.SendActivityAsync("¿Te podemos ayudar en algo mas?");
                    return await stepContext.EndDialogAsync();
                }
                else
                {
                    await ResetUserProfile(stepContext);
                    await stepContext.Context.SendActivityAsync("Error al cancelar la cita. Intenta mas tarde.");
                    return await stepContext.EndDialogAsync();

                }
            }
            else if (choice.Value.Equals("No", StringComparison.OrdinalIgnoreCase))
            {
                await _userState.ClearStateAsync(stepContext.Context);
                stepContext.Values.Clear();
                await ResetUserProfile(stepContext);
                await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                await stepContext.Context.SendActivityAsync("Cancelacion anulada. ¿Te podemos ayudar en algo mas?");
                return await stepContext.NextAsync();
            }
            else
            {
                return await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), null, cancellationToken);
            }
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
