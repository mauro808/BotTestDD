using CoreBotTestDD.Models;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Threading.Tasks;
using System;
using CoreBotTestDD.Services;
using DonDoctor.ConfigurationProvider.Core;

namespace CoreBotTestDD.Utilities
{
    public class ClientMessages
    {
        private readonly ApiCalls _apiCalls;

        public ClientMessages(ApiCalls apiCalls){
            _apiCalls = apiCalls;
            }
        public async Task<string> GetClientSha(string channelId)
        {
            using (HttpClient client = new HttpClient()) {
                try
                {
                    client.BaseAddress = new Uri(ApplicationConfigurationProvider.AppSetting("BotEngineUrl"));
                    //client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await _apiCalls.AutenticationAsync());
                    HttpResponseMessage response = await client.GetAsync($"origin?ChannelId={channelId}");

                    if (response.IsSuccessStatusCode)
                    {
                        if (response.StatusCode.Equals("204"))
                        {
                            return null;
                        }
                        string jsonResponse = await response.Content.ReadAsStringAsync();
                        JObject jsonObj = JsonConvert.DeserializeObject<JObject>(jsonResponse);
                        return jsonObj["clientSHA"]?.ToString();
                    }
                    else
                    {
                        Console.WriteLine($"Error: {response.StatusCode}");
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Excepción: {ex.Message}");
                    return null;
                }
            }
        }

        public async Task<string> GetClientMessages(string idMessage, string clientCode)
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    client.BaseAddress = new Uri(ApplicationConfigurationProvider.AppSetting("MasterRobotUrl"));
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await _apiCalls.AutenticationAsync(clientCode));
                    HttpResponseMessage response = await client.GetAsync($"RobotStep/GetByStepName?stepName={idMessage}");

                    if (response.IsSuccessStatusCode)
                    {
                        if (response.StatusCode.Equals("204"))
                        {
                            return null;
                        }
                        string jsonResponse = await response.Content.ReadAsStringAsync();
                        JObject jsonObj = JsonConvert.DeserializeObject<JObject>(jsonResponse);
                        return jsonObj["message"]?.ToString();
                    }
                    else
                    {
                        Console.WriteLine($"Error: {response.StatusCode}");
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Excepción: {ex.Message}");
                    return null;
                }
            }
        }
    }
}