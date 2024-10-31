// Generated with Bot Builder V4 SDK Template for Visual Studio CoreBot v4.22.0

using CoreBotTestDD.Bots;
using DonBot.Bots;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.ApplicationInsights.Core;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.TraceExtensions;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;

namespace CoreBotTestDD
{
    public class AdapterWithErrorHandler : CloudAdapter
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public AdapterWithErrorHandler(BotFrameworkAuthentication auth, ILogger<IBotFrameworkHttpAdapter> logger, InactivityMiddleware inactivityMiddleware, KustomerMiddleware kustomerMiddleware ,TelemetryInitializerMiddleware telemetryInitializerMiddleware , IHttpClientFactory httpClientFactory, ConversationState conversationState = default)
            : base(auth, logger)
        {
            _httpClientFactory = httpClientFactory;
            //Use(telemetryInitializerMiddleware);
            Use(inactivityMiddleware);
            //Use(kustomerMiddleware);
            OnTurnError = async (turnContext, exception) =>
            {
                // Log any leaked exception from the application.
                logger.LogError(exception, $"[OnTurnError] unhandled error : {exception.Message}");

                // Send a message to the user
                var errorMessageText = "The bot encountered an error or bug.";
                var errorMessage = MessageFactory.Text(errorMessageText, errorMessageText, InputHints.ExpectingInput);
                await turnContext.SendActivityAsync(errorMessage);

                errorMessageText = "To continue to run this bot, please fix the bot source code.";
                errorMessage = MessageFactory.Text(errorMessageText, errorMessageText, InputHints.ExpectingInput);
                await turnContext.SendActivityAsync(errorMessage);

                if (conversationState != null)
                {
                    try
                    {
                        // Delete the conversationState for the current conversation to prevent the
                        // bot from getting stuck in a error-loop caused by being in a bad state.
                        // ConversationState should be thought of as similar to "cookie-state" in a Web pages.
                        await conversationState.DeleteAsync(turnContext);
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, $"Exception caught on attempting to Delete ConversationState : {e.Message}");
                    }
                }

                // Send a trace activity, which will be displayed in the Bot Framework Emulator
                await turnContext.TraceActivityAsync("OnTurnError Trace", exception.Message, "https://www.botframework.com/schemas/error", "TurnError");
            };
        }

        public override async Task<ResourceResponse[]> SendActivitiesAsync(ITurnContext turnContext, Activity[] activities, CancellationToken cancellationToken)
        {
            foreach (var activity in activities)
            {
                // Verificamos si la actividad es un mensaje enviado por el bot
                if (activity.Type == ActivityTypes.Message && activity.From?.Role == "bot")
                {
                    var botResponse = activity.Text; // El texto que el bot está enviando al usuario
                    var recipientId = activity.Recipient.Id;
                    var conversationId = activity.Conversation.Id;
                    // Aquí puedes enviar el mensaje a Kustomer
                    await SendMessageToOrchestator(botResponse, recipientId, conversationId);
                }
            }
            // Continuar con el procesamiento normal
            return await base.SendActivitiesAsync(turnContext, activities, cancellationToken);
        }

        private async Task SendMessageToOrchestator(string message, string to, string conversationId)
        {
            var httpClient = _httpClientFactory.CreateClient();
            var url = "https://your-kustomer-instance.com/api/v1/messages";

            var content = new StringContent(JsonConvert.SerializeObject(new
            {
                Id = "Bot",
                to = to,
                body = message,
                conversation = conversationId
            }), Encoding.UTF8, "application/json");

            /**var response = await httpClient.PostAsync(url, content);
            if (!response.IsSuccessStatusCode)
            {
                // Manejar error
            }**/
        }
    }
}
