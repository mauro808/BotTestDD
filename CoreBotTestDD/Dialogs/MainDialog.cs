// Generated with Bot Builder V4 SDK Template for Visual Studio CoreBot v4.22.0

using CoreBotTestDD.CognitiveModels;
using CoreBotTestDD.Models;
using CoreBotTestDD.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CoreBotTestDD.Dialogs
{
    public class MainDialog : ComponentDialog
    {
        private readonly UserState _userState;
        private readonly ConversationState _conversationState;
        private readonly AgendarDialog _agendarDialog;
        private readonly CLUService _cluService;
        private readonly CQAService _cqaService;
        private readonly ILogger _logger;
        private readonly IStatePropertyAccessor<UserProfileModel> _userStateAccessor;

        public MainDialog(CLUService cluService, CQAService cqaService, ILogger<MainDialog> logger, UserState userState, ConversationState conversationState, AgendarDialog agendarDialog)
            : base(nameof(MainDialog))
        {
            _userState = userState ?? throw new ArgumentNullException(nameof(userState));
            _conversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));
            _cluService = cluService;
            _cqaService = cqaService;
            _logger = logger;
            _userStateAccessor = _userState.CreateProperty<UserProfileModel>("UserProfile");
            _agendarDialog = agendarDialog;

            AddDialog(agendarDialog);
            AddDialog(new TextPrompt(nameof(TextPrompt)));

            var waterfallSteps = new WaterfallStep[]
            {
                IntroStepAsync,
                ActStepAsync,
                RouterStepAsync,
                FinalStepAsync,
            };

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));
            InitialDialogId = nameof(WaterfallDialog);
        }
        private async Task<DialogTurnResult> IntroStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userProfile = await _userStateAccessor.GetAsync(stepContext.Context, () => new UserProfileModel { IsNewUser = true }, cancellationToken);
            if (userProfile.IsNewUser)
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("¡Hola! Soy un bot MVP para la verificacion de capacidades del framework Bot SDK. ¿Como podria ayudarte?"));
                userProfile.IsNewUser = false; 
            }
            await _userStateAccessor.SetAsync(stepContext.Context, userProfile, cancellationToken);
            await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
            return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> ActStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userProfile = await _userStateAccessor.GetAsync(stepContext.Context, () => new UserProfileModel(), cancellationToken);
            return new DialogTurnResult(DialogTurnStatus.Waiting);
        }

        private async Task<DialogTurnResult> RouterStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userProfile = await _userStateAccessor.GetAsync(stepContext.Context, () => new UserProfileModel { IsNewUser = true }, cancellationToken);
            var generalScore = await _cluService.AnalyzeTextAsync(stepContext.Context.Activity.Text.ToString());
            if(generalScore == "FAQs")
            {
                var response = await _cqaService.AnalyzeQuestionAsync(stepContext.Context.Activity.Text.ToString());
                await stepContext.Context.SendActivityAsync(response);
                await stepContext.Context.SendActivityAsync("¿Puedo ayudarte en algo mas?");

            }
            else
            {
                return await stepContext.BeginDialogAsync(nameof(AgendarDialog),null,  cancellationToken);
            }
            return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.ReplaceDialogAsync(InitialDialogId, default);
        }

        protected override Task<DialogTurnResult> OnContinueDialogAsync(DialogContext innerDc, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Añade lógica adicional que necesites antes de llamar al método base
            _logger.LogInformation("OnContinueDialogAsync was called.");
            
            // Llama al método base para asegurarte de que se ejecuten otras implementaciones de OnContinueDialogAsync en la pila de diálogos
            return base.OnContinueDialogAsync(innerDc, cancellationToken);
        }
    }
}
