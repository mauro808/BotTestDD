using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System;
using CoreBotTestDD.Models;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using System.Globalization;
using DonBot.Models;
using System.Linq;

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

        public async Task<ResponseCLU> AnalyzeTextAsync(string text)
        {
            text = RemoveCommasAndAccents(text);
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
                        ResponseCLU responseModel = new();
                        JObject jsonRes = JObject.Parse(jsonResponse);
                        responseModel.intent = jsonRes["result"]["prediction"]["topIntent"].ToString();
                        JArray entities = (JArray)jsonRes["result"]?["prediction"]?["entities"];
                        if (entities != null && entities.Count > 0)
                        {
                            var servicioItem = entities.FirstOrDefault(item => item["category"] != null && (item["category"].ToString() == "Servicios" || item["category"].ToString() == "Nombre"));
                            if (servicioItem != null)
                            {
                                var extraInformationService = servicioItem["extraInformation"] as JArray;
                                if (extraInformationService != null && extraInformationService.Count > 0)
                                {
                                    if (servicioItem["category"].ToString().Equals("Nombre"))
                                    {
                                        var key = servicioItem["text"].ToString();
                                        if (!string.IsNullOrEmpty(key))
                                        {
                                            responseModel.category = "Nombre";
                                            responseModel.KeyServices = key;
                                        }
                                    }
                                    else
                                    {
                                        var key = extraInformationService[0]["key"]?.ToString();
                                        if (!string.IsNullOrEmpty(key))
                                        {
                                            responseModel.category = "Servicio";
                                            responseModel.KeyServices = key;
                                        }
                                    }
                                }
                                

                            }
                            JObject firstEntity = (JObject)entities[0];
                            JArray extraInformation = (JArray)firstEntity["extraInformation"];
                            if(extraInformation != null)
                            {
                                JObject firstExtraInfo = (JObject)extraInformation[0];
                                responseModel.key = firstExtraInfo["key"]?.ToString();
                            }
                        }
                        return responseModel;
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

        public async Task<ResponseCLUPrompt> AnalyzeTextEntitiesAsync(string text)
        {
            text = RemoveCommasAndAccents(text);
            ResponseCLUPrompt responseModel = new();
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
                            foreach (var entity in entities)
                            {
                                // Validar si la entidad tiene "extraInformation"
                                var category = entity["category"]?.ToString();
                                var resolutions = (JArray)entity["resolutions"];
                                if (category != null && text != null)
                                {
                                    if (category.Equals("Nombre"))
                                    {
                                        responseModel.SubType = category;
                                        responseModel.Value = entity["text"]?.ToString();
                                    }else if (category.Equals("Fechas"))
                                    {
                                        var extraInformationDate = (JArray)entity["extraInformation"];
                                        string Datatype = extraInformationDate[0]["value"]?.ToString();
                                        if(Datatype.Equals("datetime.time"))
                                        {
                                            string timeString = resolutions[0]["value"]?.ToString();
                                            DateTime time = DateTime.Parse(timeString);
                                            responseModel.SubType = "Fechas";
                                            responseModel.Value = time.ToString("hh:mm");
                                        }else if (Datatype.Equals("datetime.daterange"))
                                        {
                                            responseModel.Value = text;
                                        }else if(entities.Count() > 1)
                                        {
                                            var secondResolution = resolutions[1];
                                            var value = secondResolution["value"]?.ToString();
                                            DateTime fecha = DateTime.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                                            CultureInfo cultura = new("es-ES");
                                            responseModel.SubType = "Fechas";
                                            responseModel.Value = fecha.ToString("dddd dd 'de' MMMM", cultura);
                                        }
                                        else
                                        {
                                            responseModel.Value = text;
                                        }
                                    }
                                }
                            }
                            // Acceder a la primera entidad
                            JObject firstEntity = (JObject)entities[0];

                            // Acceder a la primera "extraInformation" de la entidad
                            JArray extraInformation = (JArray)firstEntity["extraInformation"];

                            if (extraInformation != null && extraInformation.Count > 0)
                            {
                                JObject firstExtraInfo = (JObject)extraInformation[0];
                                string key = firstExtraInfo["key"]?.ToString();

                                // Imprimir la clave
                                responseModel.intent = key;
                                return responseModel;

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

        public async Task<string> AnalyzeTextEntitiesDataCorrectionAsync(string text)
        {
            text = RemoveCommasAndAccents(text);
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
                            JObject specificEntity = null;
                            foreach (JObject entity in entities)
                            {
                                // Reemplaza "nombre_de_la_entidad" con el nombre de la entidad que estás buscando
                                if (entity["category"]?.ToString() == "nombre_de_la_entidad")
                                {
                                    specificEntity = entity;
                                    break; // Sal del bucle una vez que encuentres la entidad
                                }
                            }
                            if (specificEntity != null)
                            {
                                string key = specificEntity["key"]?.ToString();
                                return key;
                            }
                            else
                            {
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

        static string RemoveCommasAndAccents(string text)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));

            // Eliminar comas
            string withoutCommas = text.Replace(",", "");

            // Normalizar el texto a forma de descomposición canónica
            string normalized = withoutCommas.Normalize(NormalizationForm.FormD);

            // Crear un StringBuilder para construir el texto resultante sin acentos
            StringBuilder builder = new StringBuilder();

            // Recorrer cada carácter en el texto normalizado
            foreach (char c in normalized)
            {
                // Agregar solo caracteres que no sean marcas de acento
                UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(c);
                if (category != UnicodeCategory.NonSpacingMark)
                {
                    builder.Append(c);
                }
            }

            // Devolver el texto final sin comas y sin acentos
            return builder.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
