using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System;
using CoreBotTestDD.Models;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.DataProtection.KeyManagement;

namespace CoreBotTestDD.Services
{
    public class CLUService
    {
        private readonly string apiKey;
        private readonly string cluEndpoint;

        public CLUService(AppConfiguration appConfiguration)
        {
            this.apiKey = appConfiguration.ApiKey;
            this.cluEndpoint = appConfiguration.Endpoint;
        }

        public async Task<string> AnalyzeTextAsync(string text)
        {
            try
            {
                // Configura la solicitud HTTP POST
                var request = new TextAnalisysModel
                {
                    kind = "Conversation",
                    analysisInput = new AnalysisInput
                    {
                        conversationItem = new ConversationItem
                        {
                            id = "1",
                            participantId = "1",
                            text = text
                        }
                    },
                    parameters = new Parameters
                    {
                        projectName = "DonBot-CLU",
                        deploymentName = "DonBotImp-test",
                        stringIndexType = "TextElement_V8"
                    }
                };
                string jsonRequest = Newtonsoft.Json.JsonConvert.SerializeObject(request);
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                using (HttpClient client = new HttpClient())
                {
                    // Agrega la clave de suscripción al header
                    client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey);

                    // Realiza la solicitud HTTP POST
                    HttpResponseMessage response = await client.PostAsync(cluEndpoint, content);

                    // Maneja la respuesta del servicio
                    if (response.IsSuccessStatusCode)
                    {
                        string jsonResponse = await response.Content.ReadAsStringAsync();
                        JObject jsonRes = JObject.Parse(jsonResponse);
                        string topIntent = jsonRes["result"]["prediction"]["topIntent"].ToString();
                        return topIntent;
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

        public async Task<string> AnalyzeTextEntitiesAsync(string text)
        {
            try
            {
                // Configura la solicitud HTTP POST
                var request = new TextAnalisysModel
                {
                    kind = "Conversation",
                    analysisInput = new AnalysisInput
                    {
                        conversationItem = new ConversationItem
                        {
                            id = "1",
                            participantId = "1",
                            text = text
                        }
                    },
                    parameters = new Parameters
                    {
                        projectName = "DonBot-CLU",
                        deploymentName = "DonBotImp-test",
                        stringIndexType = "TextElement_V8"
                    }
                };
                string jsonRequest = Newtonsoft.Json.JsonConvert.SerializeObject(request);
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                using (HttpClient client = new HttpClient())
                {
                    // Agrega la clave de suscripción al header
                    client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey);

                    // Realiza la solicitud HTTP POST
                    HttpResponseMessage response = await client.PostAsync(cluEndpoint, content);

                    // Maneja la respuesta del servicio
                    if (response.IsSuccessStatusCode)
                    {
                        string jsonResponse = await response.Content.ReadAsStringAsync();
                        JObject jsonRes = JObject.Parse(jsonResponse);
                        JArray entities = (JArray)jsonRes["result"]?["prediction"]?["entities"];
                        if (entities != null && entities.Count > 0)
                        {
                            // Acceder a la primera entidad
                            JObject firstEntity = (JObject)entities[0];

                            // Acceder a la primera "extraInformation" de la entidad
                            JArray extraInformation = (JArray)firstEntity["extraInformation"];

                            if (extraInformation != null && extraInformation.Count > 0)
                            {
                                JObject firstExtraInfo = (JObject)extraInformation[0];
                                string key = firstExtraInfo["key"]?.ToString();

                                // Imprimir la clave
                                Console.WriteLine($"Key: {key}");
                                return key;

                            }
                            else
                            {
                                Console.WriteLine("No extra information found.");
                                return null;
                            }
                        }
                        else
                        {
                            return null;
                        }
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
