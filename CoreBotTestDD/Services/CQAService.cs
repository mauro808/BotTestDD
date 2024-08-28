using CoreBotTestDD.Models;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Text;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using DonBot.Models;

namespace CoreBotTestDD.Services
{
    public class CQAService
    {
        private readonly string apiKey;
        private readonly string ServiceUrl;

        public CQAService(AppConfiguration appConfiguration)
        {
            this.apiKey = appConfiguration.ApiKey;
            this.ServiceUrl = appConfiguration.ServiceUrl;
        }

        public async Task<MessageRequestQNA> AnalyzeQuestionAsync(string text)
        {
            try
            {
                // Configura la solicitud HTTP POST
                var request = new CQAModel
                {
                    top = 3,
                    question = text,
                    includeUnstructuredSources = true,
                    confidenceScoreThreshold = "0.4",
                    answerSpanRequest = new AnswerSpanRequest
                    {
                        enable = true,
                        topAnswersWithSpan = 1,
                        confidenceScoreThreshold = "0.7"
                    }
                };
                string jsonRequest = Newtonsoft.Json.JsonConvert.SerializeObject(request);
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                using (HttpClient client = new HttpClient())
                {
                    // Agrega la clave de suscripción al header
                    client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey);

                    // Realiza la solicitud HTTP POST
                    HttpResponseMessage response = await client.PostAsync(ServiceUrl, content);

                    // Maneja la respuesta del servicio
                    if (response.IsSuccessStatusCode)
                    {
                        string jsonResponse = await response.Content.ReadAsStringAsync();
                        JObject jsonRes = JObject.Parse(jsonResponse);
                        var firstAnswer = jsonRes["answers"][0];
                        MessageRequestQNA QuestionAns = new();
                        var QuestionId = firstAnswer["id"].ToString();
                        if(QuestionId == "-1")
                        {
                            return null;
                        }
                        QuestionAns.text = firstAnswer["answer"].ToString();
                        JToken metadata = firstAnswer["metadata"]?["state"];
                        if (metadata != null)
                        {
                            QuestionAns.metadata = firstAnswer["metadata"]?["state"].ToString();
                        }
                        return QuestionAns;
                    }
                    else
                    {
                        throw new Exception($"Error al hacer la solicitud: {response.StatusCode}");
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                // Manejo de errores de red
                throw new Exception("Error de red al hacer la solicitud HTTP", ex);
            }
            catch (Exception ex)
            {
                // Manejo de otras excepciones
                throw new Exception("Error al hacer la solicitud", ex);
            }
        }
    }
}
