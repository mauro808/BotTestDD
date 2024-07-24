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

        public async Task<JObject> GetUserByIdAsync(string DocumentType, string DocumentId)
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
                        return JsonConvert.DeserializeObject<JObject>(respuesta);
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

        public async Task<bool> PostCreateCitaAsync(UserProfileModel userProfile, object data)
        {
            var url = "https://api-appointments-test-001.azurewebsites.net/Appointments";
            string dateTimeString = (string)data.GetType().GetProperty("DateTime").GetValue(data);
            DateTime dateTime = DateTime.Parse(dateTimeString);
            DateTime newDateTime = dateTime;
            var appointmentTypeIdProp = data.GetType().GetProperty("AppointmentTypeId");
            var userIdProp = data.GetType().GetProperty("UserId");
            var officeIdProp = data.GetType().GetProperty("OfficeId");
            string appointmentTypeIdStr = appointmentTypeIdProp?.GetValue(data) as string;
            string userIdStr = userIdProp?.GetValue(data) as string;
            string officeIdStr = officeIdProp?.GetValue(data) as string;
            if (!int.TryParse(appointmentTypeIdStr, out int appointmentTypeId))
            {
                throw new ArgumentException("AppointmentTypeId no es un entero válido.");
            }

            if (!int.TryParse(userIdStr, out int userId))
            {
                throw new ArgumentException("UserId no es un entero válido.");
            }

            if (!int.TryParse(officeIdStr, out int officeId))
            {
                throw new ArgumentException("OfficeID no es un entero válido.");
            }
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await AutenticationAsync());
                    var parametros = new
                    {
                        AppointmentTypeID = appointmentTypeId,
                        UserID = int.Parse(userProfile.UserId),
                        InsuranceID = int.Parse(userProfile.Aseguradora),
                        DoctorID = userId,
                        ServiceID = int.Parse(userProfile.Servicios),
                        DateFrom = dateTime,
                        To = newDateTime.ToString("HH:mm:ss tt"),
                        State = "P",
                        Message = "",
                        InsurancePlanID = int.Parse(userProfile.PlanAseguradora),
                        OfficeID = officeId,
                        Telemedicine = false,
                        SpecialtyID = int.Parse(userProfile.especialidad),
                        Origin = 3,
                    };
                    var jsonContent = JsonConvert.SerializeObject(parametros);
                    var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await client.PostAsync(url, httpContent);

                    if (response.IsSuccessStatusCode)
                    {
                        if (response.StatusCode.Equals("204"))
                        {
                            return false;
                        }
                        string jsonResponse = await response.Content.ReadAsStringAsync();
                        bool responseRef = await PostRefreshCitaAsync(JObject.Parse(jsonResponse));
                        if(responseRef == true)
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Error: {response.StatusCode}");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Excepción: {ex.Message}");
                    return false;
                }
            }

        }

        public async Task<bool> PostRefreshCitaAsync(JObject obj)
        {
            var url = "https://api-appointments-test-001.azurewebsites.net/Appointments";
            bool ResultRef = false;
            string AppointmentId = obj["appointmentID"]?.ToString();
            var RemoteId = obj["remoteID"]?.ToString();
            string userIdStr = obj["userID"]?.ToString();
            bool isValid = int.TryParse(userIdStr, out int userId);
            var origin = obj["origin"]?.ToString();
            bool StatisticsIgnore = bool.TryParse(obj["statisticsIgnore"].ToString(), out bool result) ? result : false;
            if (!int.TryParse(AppointmentId, out int appointmentId))
            {
                throw new ArgumentException("AppointmentTypeId no es un entero válido.");
            }

            if (!int.TryParse(origin, out int originIn))
            {
                throw new ArgumentException("UserId no es un entero válido.");
            }
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await AutenticationAsync());
                    var parametros = new Dictionary<string, object>
                    {
                       { "AppointmentID", appointmentId },
                       { "RemoteID", null},
                        { "Origin", originIn },
                        { "StatisticsIgnore", StatisticsIgnore },
                        { "State", "A" },
                        { "Confirmed", true },
                        { "UserID", userId}
                       
                    };              
                    var jsonContent = JsonConvert.SerializeObject(parametros);
                    var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await client.PutAsync(url, httpContent);

                    if (response.IsSuccessStatusCode)
                    {
                        if (response.StatusCode.Equals("204"))
                        {
                            return false;
                        }
                        return true; ;
                    }
                    else
                    {
                        Console.WriteLine($"Error: {response.StatusCode}");
                        return ResultRef;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Excepción: {ex.Message}");
                    return false ;
                }
            }

        }
    }
    }
