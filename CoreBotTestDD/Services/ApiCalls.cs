using CoreBotTestDD.Models;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Collections.Generic;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Drawing;
using System.Numerics;

namespace CoreBotTestDD.Services
{
    public class ApiCalls
    {
        public async Task<string> AutenticationAsync()
        {
            string json = "{\"Username\": \"dondoctor-robot\", \"Password\": \"PrU3b4Dd*\", \"clientSha\": \"6b5e37ce5ee0ef483fd1fe928ad28d283109bfcf6bd09f29a6682b5fe8fef17b\"}";
            string url = "https://api-security-test-001.azurewebsites.net/Login";
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAsync(url, httpContent);

                    if (response.IsSuccessStatusCode)
                    {
                        string respuesta = await response.Content.ReadAsStringAsync();
                        var JsonObj = JObject.Parse(respuesta);
                        string token = JsonObj["token"].ToString();
                        return token;
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

        public async Task<string> GetUserByIdAsync(string DocumentType, string DocumentId)
        {
            string url = "https://api-users-test-001.azurewebsites.net/User?documentTypeId=" + DocumentType + "&documentNumber=" + DocumentId + "&includeAttributes=false";
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await AutenticationAsync());
                    HttpResponseMessage response = await client.GetAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        if (response.StatusCode.Equals("204"))
                        {
                            return null;
                        }
                        string respuesta = await response.Content.ReadAsStringAsync();
                        JObject obj = JObject.Parse(respuesta);
                        string name = (string)obj["name"];

                        return name;
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

        public async Task<List<JObject>> GetInsurancePlanAsync(string InsuranceId)
        {
            string url = "https://api-plans-prod-001.azurewebsites.net/InsurancePlan/Filters?insuranceId=" + InsuranceId;
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await AutenticationAsync());
                    HttpResponseMessage response = await client.GetAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        if (response.StatusCode.Equals("204"))
                        {
                            return null;
                        }
                        string jsonResponse = await response.Content.ReadAsStringAsync();
                        return JsonConvert.DeserializeObject<List<JObject>>(jsonResponse);
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
        public async Task<List<JObject>> GetDocumentTypeAsync()
        {
            string ServicesEndpoint = "https://api-documenttypes-test-001.azurewebsites.net/DocumentTypes";
            try
            {

                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await AutenticationAsync());
                    HttpResponseMessage response = await client.GetAsync(ServicesEndpoint);

                    // Maneja la respuesta del servicio
                    if (response.IsSuccessStatusCode)
                    {
                        string jsonResponse = await response.Content.ReadAsStringAsync();
                        return JsonConvert.DeserializeObject<List<JObject>>(jsonResponse);
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

        public async Task<List<JObject>> GetAseguradorasAsync(string text)
        {
            string ServicesEndpoint = "https://api-insurers-test-001.azurewebsites.net/Insurance/GetByName?name=" + text;
            try
            {

                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await AutenticationAsync());
                    HttpResponseMessage response = await client.GetAsync(ServicesEndpoint);

                    // Maneja la respuesta del servicio
                    if (response.IsSuccessStatusCode)
                    {
                        var menuBuilder = new StringBuilder();
                        int index = 1;
                        //menuBuilder.AppendLine("Encontramos las siguientes aseguradoras: ");
                        string data = await response.Content.ReadAsStringAsync();
                        return JsonConvert.DeserializeObject<List<JObject>>(data);
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

        public async Task<List<JObject>> GetServicesAsync(string text)
        {
            string ServicesEndpoint = "https://api-services-prod-001.azurewebsites.net/Services/GetByName?name=" + text;
            try
            {

                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await AutenticationAsync());
                    HttpResponseMessage response = await client.GetAsync(ServicesEndpoint);

                    // Maneja la respuesta del servicio
                    if (response.IsSuccessStatusCode)
                    {
                        string data = await response.Content.ReadAsStringAsync();
                        return JsonConvert.DeserializeObject<List<JObject>>(data);
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

        public async Task<List<JObject>> GetEspecialidadAsync(string ServiceId)
        {
            string url = "https://api-specialities-test-001.azurewebsites.net/Specialties/Service?serviceid=" + ServiceId;
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await AutenticationAsync());
                    HttpResponseMessage response = await client.GetAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        if (response.StatusCode.Equals("204"))
                        {
                            return null;
                        }
                        string jsonResponse = await response.Content.ReadAsStringAsync();
                        return JsonConvert.DeserializeObject<List<JObject>>(jsonResponse);
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

        public async Task<List<JObject>> GetScheduleAvailabilityAsync(UserProfileModel userProfile)
        {
            var baseUrl = "https://api-schedules-test-001.azurewebsites.net/ScheduleAvailability";
            var query = new List<string>
                {
                    $"size=30",
                    $"page=0",
                    $"planId={userProfile.PlanAseguradora}",
                    $"serviceId={userProfile.Servicios}",
                    $"specialtyId={userProfile.especialidad}",
                    $"available=true"
                };
            var fullUrl = $"{baseUrl}?{string.Join("&", query)}";
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await AutenticationAsync());
                    HttpResponseMessage response = await client.GetAsync(fullUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        if (response.StatusCode.Equals("204"))
                        {
                            return null;
                        }
                        string jsonResponse = await response.Content.ReadAsStringAsync();
                        return JsonConvert.DeserializeObject<List<JObject>>(jsonResponse);
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

        public async Task<List<JObject>> PostCreateCitaAsync(UserProfileModel userProfile, object data)
        {
            var url = "https://api.ejemplo.com/endpoint";

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await AutenticationAsync());
                    var parametros = new
                    {
                        AppointmentTypeID = "valor1",
                        UserID = "valor2",
                        InsuranceID = "",
                        DoctorID = "",
                        ServiceID = "",
                        DateFrom = "",
                        To = "",
                        State = "",
                        InsurancePlanID = "",
                        OfficeID = "",
                        Telemedicine = "",
                        SpecialtyID = "",
                        Origin = "",
                        // Agrega más parámetros según sea necesario
                    };
                    HttpResponseMessage response = await client.GetAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        if (response.StatusCode.Equals("204"))
                        {
                            return null;
                        }
                        string jsonResponse = await response.Content.ReadAsStringAsync();
                        return JsonConvert.DeserializeObject<List<JObject>>(jsonResponse);
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
