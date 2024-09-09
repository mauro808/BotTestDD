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
using System.Collections.Generic;

namespace DonBot.Dialogs
{
    public class ListaEsperaDialog : ComponentDialog
    {
        private readonly UserState _userState;
        private readonly ApiCalls _apiCalls;
        private readonly ConversationState _conversationState;
        private readonly RegisterDialog _registerDialog;
        private readonly ILogger _logger;
        private readonly IStatePropertyAccessor<DialogState> _dialogStateAccessor;
        private readonly IStatePropertyAccessor<UserProfileModel> _userStateAccessor;

        public ListaEsperaDialog(ILogger<MainDialog> logger, UserState userState, ConversationState conversationState, ApiCalls apiCalls, CustomChoicePrompt customChoicePrompt)
          : base(nameof(ListaEsperaDialog))
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
                HandleRegisterSelectionAsync
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
                Prompt = MessageFactory.Text("¿Deseas que te registremos en la lista de espera?"),
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
            var options = (IEnumerable<dynamic>)stepContext.Values["Choice"];
            var choice = (FoundChoice)stepContext.Result;
            if (choice != null)
            {
                if (choice.Value.Equals("No", StringComparison.OrdinalIgnoreCase))
                {
                    stepContext.Context.Activity.Text = null;
                    userProfile.Choice = false;
                    await ResetUserProfile(stepContext);
                    await _conversationState.SaveChangesAsync(stepContext.Context);
                    await stepContext.Context.SendActivityAsync("¿Puedo ayudarte en algo más?", cancellationToken: cancellationToken);
                    var cancellationReason = new { Reason = DialogReason.CancelCalled };
                    await stepContext.CancelAllDialogsAsync(cancellationToken);
                    return await stepContext.EndDialogAsync(cancellationReason, cancellationToken);
                }
                else
                {
                    bool response = await _apiCalls.PostCreateListaEsperaAsync(userProfile);
                    if (response)
                    {
                        await stepContext.Context.SendActivityAsync("Te hemos agregado correctamente a la lista de espera.", cancellationToken: cancellationToken);
                        await stepContext.Context.SendActivityAsync("¿Puedo ayudarte en algo más?", cancellationToken: cancellationToken);
                        var cancellationReason = new { Reason = DialogReason.CancelCalled };
                        return await stepContext.EndDialogAsync(cancellationReason, cancellationToken);

                    }
                    else
                    {
                        await stepContext.Context.SendActivityAsync("Te hemos agregado correctamente a la lista de espera.", cancellationToken: cancellationToken);
                        await stepContext.Context.SendActivityAsync("¿Puedo ayudarte en algo más?", cancellationToken: cancellationToken);
                        var cancellationReason = new { Reason = DialogReason.CancelCalled };
                        return await stepContext.EndDialogAsync(cancellationReason, cancellationToken);
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

            await _userStateAccessor.SetAsync(context.Context, userProfile, cancellationToken);
            await _userState.SaveChangesAsync(context.Context, false, cancellationToken);
        }
    }
}
