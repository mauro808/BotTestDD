using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using IMiddleware = Microsoft.Bot.Builder.IMiddleware;

namespace DonBot.Bots
{
    public class KustomerMiddleware : IMiddleware
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public KustomerMiddleware(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task OnTurnAsync(ITurnContext turnContext, NextDelegate next, CancellationToken cancellationToken = default)
        {
            // Continuar con la siguiente parte del procesamiento
            await next(cancellationToken);

            // Verificar si el bot está enviando un mensaje
            if (turnContext.Activity.Type == ActivityTypes.Message && turnContext.Activity.Recipient != null)
            {
                var message = turnContext.Activity.Text;

                // Enviar la respuesta del bot a Kustomer
                await SendMessageToKustomer(message, turnContext.Activity.Recipient.Id, turnContext.Activity.Conversation.Id);
            }
        }

        private async Task SendMessageToKustomer(string message, string to, string conversationId)
        {
            var httpClient = _httpClientFactory.CreateClient();
            var url = "https://your-kustomer-instance.com/api/v1/messages";
            var content = new StringContent(JsonConvert.SerializeObject(new
            {
                from = "Bot",
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
