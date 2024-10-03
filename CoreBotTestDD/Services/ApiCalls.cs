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
using DonDoctor.ConfigurationProvider.Core;
using System.Security.Policy;
using System.Text.Json.Nodes;
using System.Linq;
using System.Globalization;
using Microsoft.AspNetCore.Http;

namespace CoreBotTestDD.Services
{
    public class ApiCalls
    {
        private readonly string UrlDocumentId = "https://api-documenttypes-test-001.azurewebsites.net/DocumentTypes?documentTypeId=";
        private readonly string UrlAfiliationTypeId = "https://api-affiliation-types-test-001.azurewebsites.net/AffiliationType/Id?id=";
        private readonly string UrlMaritalStatusId = "https://api-marital-status-test-001.azurewebsites.net/MaritalStatus/Filters?id=";
        private readonly string UrlPatientTypeId = "https://api-patient-types-test-001.azurewebsites.net/PatientType/Id?id=1&remoteId=";
        private readonly string UrlAseguradoraId = "https://api-insurers-test-001.azurewebsites.net/Insurance?insuranceID=";
        private readonly string UrlAseguradoraPlanId = "https://api-plans-prod-001.azurewebsites.net/InsurancePlan?InsurancePlanID=1&includeQuotes=true";
        private readonly string UrlCiudadId = "https://api-locations-prod-001.azurewebsites.net/Cities?cityID=";
        private readonly string UrlGenderId = "https://api-genders-test-001.azurewebsites.net/Genders?GenderID=";
        private readonly string UrlPreparation = "https://api-preparations-test-001.azurewebsites.net/Preparations/Service?id=";
        public async Task<string> AutenticationAsync(string clientCode)
        {
            string url = "https://api-security-test-001.azurewebsites.net/Login";
            var jsonObject = new
            {
                Username = ApplicationConfigurationProvider.AppSetting("PartnerUserName"),
                Password = ApplicationConfigurationProvider.AppSetting("PartnerPassword"),
                clientSha = clientCode
            };
            string json = JsonConvert.SerializeObject(jsonObject);
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

        public async Task<JObject> GetUserByIdAsync(string DocumentType, string DocumentId, string clientCode)
        {
            string url = "https://api-users-test-001.azurewebsites.net/User?documentTypeId=" + DocumentType + "&documentNumber=" + DocumentId + "&includeAttributes=false";
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await AutenticationAsync(clientCode));
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

        public async Task<List<JObject>> GetGenderListAsync(string clientCode)
        {
            string ServicesEndpoint = "https://api-genders-test-001.azurewebsites.net/Genders/List";
            try
            {

                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await AutenticationAsync(clientCode));
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

        public async Task<List<JObject>> GetPatientTypeListAsync(string clientCode)
        {
            string ServicesEndpoint = "https://api-patient-types-test-001.azurewebsites.net/PatientType/List";
            try
            {

                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await AutenticationAsync(clientCode));
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

        public async Task<List<JObject>> GetVariableNameAsync(string clientCode)
        {
            string ServicesEndpoint = "https://api-patient-types-test-001.azurewebsites.net/PatientType/List";
            try
            {

                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await AutenticationAsync(clientCode));
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

        public async Task<List<JObject>> GetMaritalStatusAsync(string clientCode)
        {
            string ServicesEndpoint = "https://api-marital-status-test-001.azurewebsites.net/MaritalStatus/List";
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await AutenticationAsync(clientCode));
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

        public async Task<List<JObject>> GetAfiliationTypeAsync(string clientCode)
        {
            string ServicesEndpoint = "https://api-affiliation-types-test-001.azurewebsites.net/AffiliationType/List";
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await AutenticationAsync(clientCode));
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

        public async Task<List<JObject>> GetCityListAsync(string clientCode, string ciudad)
        {
            string ServicesEndpoint = "https://api-locations-prod-001.azurewebsites.net/Cities/GetByName?name=" + ciudad;
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await AutenticationAsync(clientCode));
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

        public async Task<List<JObject>> GetInsurancePlanAsync(string InsuranceId, string clientCode)
        {
            string url = "https://api-plans-prod-001.azurewebsites.net/InsurancePlan/Filters?insuranceId=" + InsuranceId;
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await AutenticationAsync(clientCode));
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
        public async Task<List<JObject>> GetDocumentTypeAsync(string clientCode)
        {
            string ServicesEndpoint = "https://api-documenttypes-test-001.azurewebsites.net/DocumentTypes";
            try
            {

                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await AutenticationAsync(clientCode));
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

        public async Task<List<JObject>> GetAseguradorasAsync(string text, string clientCode)
        {
            string ServicesEndpoint = "https://api-insurers-test-001.azurewebsites.net/Insurance/GetByName?name=" + text;
            try
            {

                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await AutenticationAsync(clientCode));
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

        public async Task<List<JObject>> GetServicesAsync(string text, string clientCode)
        {
            string ServicesEndpoint = "https://api-services-prod-001.azurewebsites.net/Services/GetByName?name=" + text;
            try
            {

                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await AutenticationAsync(clientCode));
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

        public async Task<List<JObject>> GetServicesByDoctorAsync(string text, string clientCode)
        {
            string ServicesEndpoint = "https://api-services-test-001.azurewebsites.net/Services/GetFilters?userId=" + text;
            try
            {

                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await AutenticationAsync(clientCode));
                    HttpResponseMessage response = await client.GetAsync(ServicesEndpoint);

                    // Maneja la respuesta del servicio
                    if (response.IsSuccessStatusCode)
                    {
                        string data = await response.Content.ReadAsStringAsync();
                        List<JObject> jsonObjectList = JsonConvert.DeserializeObject<List<JObject>>(data);
                        List<JObject> filteredList = jsonObjectList.Where(obj => obj["hidden"]?.ToObject<bool>() == false)
                        .ToList();
                        return filteredList;
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

        public async Task<List<JObject>> GetEspecialidadAsync(string ServiceId, string clientCode)
        {
            string url = "https://api-specialities-test-001.azurewebsites.net/Specialties/Service?serviceid=" + ServiceId;
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await AutenticationAsync(clientCode));
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

        public async Task<List<JObject>> GetCitasByPatientAsync(string ServiceId, string clientCode)
        {
            string url = "https://api-appointments-test-001.azurewebsites.net/Appointments/Patient?patientID=" + ServiceId;
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await AutenticationAsync(clientCode));
                    HttpResponseMessage response = await client.GetAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        if (response.StatusCode.Equals("204"))
                        {
                            return null;
                        }
                        string data = await response.Content.ReadAsStringAsync();
                        List<JObject> jsonObjectList = JsonConvert.DeserializeObject<List<JObject>>(data);
                        List<JObject> filteredList = jsonObjectList
                        .Where(obj => obj["state"]?.ToObject<string>() == "A" && obj["appointmentTypeName"]?.ToObject<string>() != "" && obj["appointmentTypeName"]?.ToObject<string>() != null)
                        .ToList();
                        return filteredList;
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

        public async Task<JObject> GetCitasByIdAsync(string citaId, string clientCode)
        {
            string url = "https://api-appointments-test-001.azurewebsites.net/Appointments?appointmentID=" + citaId;
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await AutenticationAsync(clientCode));
                    HttpResponseMessage response = await client.GetAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        if (response.StatusCode.Equals("204"))
                        {
                            return null;
                        }
                        string jsonResponse = await response.Content.ReadAsStringAsync();
                        return JsonConvert.DeserializeObject<JObject>(jsonResponse);
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

        public async Task<List<JObject>> GetScheduleAvailabilityAsync(UserProfileModel userProfile, string baseSearch)
        {
            var baseUrl = "https://api-schedules-test-001.azurewebsites.net/ScheduleAvailability";
            DateTime fechaActual = DateTime.Now;
            string fechaFormateada;
            var query = new List<string>
                {
                    $"size=30",
                    $"page=0",
                    $"planId={userProfile.PlanAseguradora}",
                    $"serviceId={userProfile.Servicios}",
                    $"specialtyId={userProfile.especialidad}",
                    $"available=true"
                };
            switch (baseSearch)
            {
                case "2":
                    int diasParaLunes = ((int)DayOfWeek.Monday - (int)fechaActual.DayOfWeek + 7) % 7;
                    if (diasParaLunes == 0)
                    {
                        diasParaLunes = 7;
                    };
                    DateTime proximoLunes = fechaActual.AddDays(diasParaLunes);
                    fechaFormateada = proximoLunes.ToString("yyyy-MM-ddTHH:mm:ss");
                    query.Add($"DateFrom={fechaFormateada}");
                    break;
                case "3":
                    DateTime primerDiaProximoMes = new DateTime(fechaActual.Year, fechaActual.Month, 1).AddMonths(1);
                    fechaFormateada = primerDiaProximoMes.ToString("yyyy-MM-ddTHH:mm:ss");
                    query.Add($"DateFrom={fechaFormateada}");
                    break;

            }
            var fullUrl = $"{baseUrl}?{string.Join("&", query)}";
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await AutenticationAsync(userProfile.CodeCompany));
                    HttpResponseMessage response = await client.GetAsync(fullUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        if (response.StatusCode.Equals("204"))
                        {
                            return null;
                        }
                        string jsonResponse = await response.Content.ReadAsStringAsync();
                        List<JObject> schedules = JArray.Parse(jsonResponse).ToObject<List<JObject>>();
                        HashSet<string> uniqueDates = new HashSet<string>();
                        List<JObject> filteredSchedules = new List<JObject>();
                        CultureInfo spanishCulture = new CultureInfo("es-ES");
                        foreach (var schedule in schedules)
                        {
                            // Obtener la fecha sin la hora
                            DateTime dateTime = DateTime.Parse(schedule["dateTime"].ToString());
                            string formattedDate = dateTime.ToString("dddd dd 'de' MMMM", spanishCulture);

                            // Si la fecha no está en el HashSet, agregarla y agregar el objeto a la lista
                            if (uniqueDates.Add(formattedDate))
                            {
                                // Actualizar el campo "dateTime" solo con la fecha (opcional)
                                schedule["dateTime"] = formattedDate;
                                filteredSchedules.Add(schedule);
                            }
                        }
                        return filteredSchedules;
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

        public async Task<List<JObject>> GetScheduleAvailabilitySpecificAsync(UserProfileModel userProfile, string fecha)
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
            DateTime parsedDate = DateTime.ParseExact(fecha, "dddd dd 'de' MMMM", new CultureInfo("es-ES"));
            string originalFormat = parsedDate.ToString("yyyy-MM-ddTHH:mm:ss");
            query.Add($"DateFrom={originalFormat}");
            query.Add($"DateTo={originalFormat}");
            var fullUrl = $"{baseUrl}?{string.Join("&", query)}";
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await AutenticationAsync(userProfile.CodeCompany));
                    HttpResponseMessage response = await client.GetAsync(fullUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        if (response.StatusCode.Equals("204"))
                        {
                            return null;
                        }
                        string jsonResponse = await response.Content.ReadAsStringAsync();
                        List<JObject> schedules = JArray.Parse(jsonResponse).ToObject<List<JObject>>();
                        foreach (var schedule in schedules)
                        {
                            // Obtener la fecha sin la hora
                            DateTime dateTime = DateTime.Parse(schedule["dateTime"].ToString());
                            string formattedTime = dateTime.ToString("hh:mm tt");

                            // Si la fecha no está en el HashSet, agregarla y agregar el objeto a la lista
                            schedule["dateTime"] = formattedTime;
                        }
                        return schedules;
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

        public async Task<List<JObject>> GetScheduleAvailabilityByMonthAsync(UserProfileModel userProfile, int mes)
        {
            var baseUrl = "https://api-schedules-test-001.azurewebsites.net/ScheduleAvailability";
            int anioSeleccionado;
            int anioActual = DateTime.Now.Year;
            int mesActual = DateTime.Now.Month;
            if (mes < mesActual)
            {
                anioSeleccionado = anioActual + 1;
            }
            else
            {
                anioSeleccionado = anioActual;
            }
            DateTime fechaPrimerDia = new DateTime(anioSeleccionado, mes, 1);
            string fechaFormateada = fechaPrimerDia.ToString("yyyy-MM-ddTHH:mm:ss");
            var query = new List<string>
                {
                    $"size=30",
                    $"page=0",
                    $"planId={userProfile.PlanAseguradora}",
                    $"serviceId={userProfile.Servicios}",
                    $"specialtyId={userProfile.especialidad}",
                    $"available=true"
                };
            query.Add($"DateFrom={fechaFormateada}");
            var fullUrl = $"{baseUrl}?{string.Join("&", query)}";
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await AutenticationAsync(userProfile.CodeCompany));
                    HttpResponseMessage response = await client.GetAsync(fullUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        if (response.StatusCode.Equals("204"))
                        {
                            return null;
                        }
                        string jsonResponse = await response.Content.ReadAsStringAsync();
                        List<JObject> schedules = JArray.Parse(jsonResponse).ToObject<List<JObject>>();
                        HashSet<string> uniqueDates = new HashSet<string>();
                        List<JObject> filteredSchedules = new List<JObject>();
                        CultureInfo spanishCulture = new CultureInfo("es-ES");
                        foreach (var schedule in schedules)
                        {
                            // Obtener la fecha sin la hora
                            DateTime dateTime = DateTime.Parse(schedule["dateTime"].ToString());
                            string formattedDate = dateTime.ToString("dddd dd 'de' MMMM", spanishCulture);

                            // Si la fecha no está en el HashSet, agregarla y agregar el objeto a la lista
                            if (uniqueDates.Add(formattedDate))
                            {
                                // Actualizar el campo "dateTime" solo con la fecha (opcional)
                                schedule["dateTime"] = formattedDate;
                                filteredSchedules.Add(schedule);
                            }
                        }
                        return filteredSchedules;
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

        public async Task<List<JObject>> GetScheduleAvailabilityByDoctorAsync(UserProfileModel userProfile)
        {
            var baseUrl = "https://api-schedules-test-001.azurewebsites.net/ScheduleAvailability";
            var query = new List<string>
                {
                    $"size=30",
                    $"page=0",
                    $"planId={userProfile.PlanAseguradora}",
                    $"serviceId={userProfile.Servicios}",
                    $"specialtyId={userProfile.especialidad}",
                    $"doctorId={userProfile.DoctorId}",
                    $"available=true"
                };
            var fullUrl = $"{baseUrl}?{string.Join("&", query)}";
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await AutenticationAsync(userProfile.CodeCompany));
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

        public async Task<string> PostCreateCitaAsync(UserProfileModel userProfile, object data, object hour)
        {
            var url = "https://api-appointments-test-001.azurewebsites.net/Appointments";
            string dateTimeString = (string)data.GetType().GetProperty("DateTime").GetValue(data);
            string dateFormat = "dddd dd 'de' MMMM";
            string[] timeFormats = { "hh:mm", "hh:mm tt", "HH:mm" };
            string inputTime = (string)hour.GetType().GetProperty("DateTime").GetValue(hour);
            DateTime timePart;
            DateTime.TryParseExact(inputTime, timeFormats, new CultureInfo("en-US"), DateTimeStyles.None, out timePart);
            DateTime datePart = DateTime.ParseExact((string)data.GetType().GetProperty("DateTime").GetValue(data), dateFormat, new CultureInfo("es-ES"));
            DateTime combinedDateTime = new DateTime(datePart.Year, datePart.Month, datePart.Day,
                                                 timePart.Hour, timePart.Minute, timePart.Second);
            string formattedDateTime = combinedDateTime.ToString("yyyy-MM-ddTHH:mm:ss");
            string formattedDateTimeTo = combinedDateTime.AddMinutes(30).ToString("yyyy-MM-ddTHH:mm:ss");
            var appointmentTypeIdProp = data.GetType().GetProperty("AppointmentTypeId");
            var userIdProp = data.GetType().GetProperty("UserId");
            var officeIdProp = data.GetType().GetProperty("OfficeId");
            string appointmentTypeIdStr = appointmentTypeIdProp?.GetValue(data) as string;
            string DoctorId = userIdProp?.GetValue(data) as string;
            string officeIdStr = officeIdProp?.GetValue(data) as string;
            if (!int.TryParse(appointmentTypeIdStr, out int appointmentTypeId))
            {
                throw new ArgumentException("AppointmentTypeId no es un entero válido.");
            }

            if (!int.TryParse(DoctorId, out int DoctorID))
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
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await AutenticationAsync(userProfile.CodeCompany));
                    var parametros = new
                    {
                        AppointmentTypeID = appointmentTypeId,
                        UserID = int.Parse(userProfile.UserId),
                        InsuranceID = int.Parse(userProfile.Aseguradora),
                        DoctorId = DoctorID,
                        ServiceID = int.Parse(userProfile.Servicios),
                        DateFrom = formattedDateTime,
                        To = formattedDateTimeTo,
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
                            return "false";
                        }else if (response.StatusCode.Equals("400"))
                        {
                            return "limited";
                        }
                        string jsonResponse = await response.Content.ReadAsStringAsync();
                        bool responseRef = await PostRefreshCitaAsync(JObject.Parse(jsonResponse), userProfile.CodeCompany);
                        if(responseRef == true)
                        {
                            return "true";
                        }
                        else
                        {
                            return "false";
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Error: {response.StatusCode}");
                        return "false";
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Excepción: {ex.Message}");
                    return "false";
                }
            }

        }

        public async Task<bool> PostRefreshCitaAsync(JObject obj, string clientCode)
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
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await AutenticationAsync(clientCode));
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

        public async Task<bool> PostCancelCitaAsync(string citaId, string userId ,string clientCode)
        {
            var url = "https://api-appointments-test-001.azurewebsites.net/Appointments";
            JObject citaData = await GetCitasByIdAsync(citaId, clientCode);
            string AppointmentId = citaData["appointmentID"].ToString();
            bool Confirmed = bool.TryParse(citaData["confirmed"].ToString(), out bool result) ? result : false;
            //var origin = citaData["origin"].ToString();
            bool StatisticsIgnore = bool.TryParse(citaData["statisticsIgnore"].ToString(), out bool resultStatistic) ? resultStatistic : false;

            if (!int.TryParse(AppointmentId, out int appointmentId))
            {
                throw new ArgumentException("AppointmentTypeId no es un entero válido.");
            }
            if (!int.TryParse(userId, out int UserId))
            {
                throw new ArgumentException("AppointmentTypeId no es un entero válido.");
            }

            /*if (!int.TryParse(origin, out int originIn))
            {
                throw new ArgumentException("UserId no es un entero válido.");
            }*/
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await AutenticationAsync(clientCode));
                    var parametros = new Dictionary<string, object>
                    {
                       { "AppointmentID", appointmentId },
                        { "StatisticsIgnore", StatisticsIgnore },
                        { "State", "C" },
                        { "Origin", 3 },
                        { "UserId", UserId },
                        { "Confirmed", Confirmed }

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


        public async Task<string> GetVariablesNameAsync(string urlExt, string clientCode, string Id)
        {
            string url;
            if(urlExt == UrlAseguradoraPlanId)
            {
                url = "https://api-plans-prod-001.azurewebsites.net/InsurancePlan?InsurancePlanID="+Id+"&includeQuotes=true";
            }
            else
            {
                url = urlExt + Id;
            }
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await AutenticationAsync(clientCode));
                    HttpResponseMessage response = await client.GetAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        if (response.StatusCode.Equals("204"))
                        {
                            return null;
                        }
                        string jsonResponse = await response.Content.ReadAsStringAsync();
                        JObject data = JsonConvert.DeserializeObject<JObject>(jsonResponse);
                        return data["name"]?.ToString();
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

        public async Task<string> GetInsurancePlanByIdAsync(string clientCode, string Id)
        {
            string url;
            url = "https://api-plans-prod-001.azurewebsites.net/InsurancePlan?InsurancePlanID=" + Id + "&includeQuotes=true";
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await AutenticationAsync(clientCode));
                    HttpResponseMessage response = await client.GetAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        if (response.StatusCode.Equals("204"))
                        {
                            return null;
                        }
                        string jsonResponse = await response.Content.ReadAsStringAsync();
                        JObject data = JsonConvert.DeserializeObject<JObject>(jsonResponse);
                        string insurance = data["insurance"]?["id"].ToString();
                        return insurance;
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

        public async Task<string> GetUserDataCreationAsync(UserProfileModel userData)
        {
            CitaModel citaModel = new()
            {
                Gender = await GetVariablesNameAsync(UrlGenderId, userData.CodeCompany, userData.Gender),
                AfiliationType = await GetVariablesNameAsync(UrlAfiliationTypeId, userData.CodeCompany, userData.Affiliation),
                DocumentType = await GetVariablesNameAsync(UrlDocumentId, userData.CodeCompany, userData.DocumentType),
                City = await GetVariablesNameAsync(UrlCiudadId, userData.CodeCompany, userData.City),
                Insurance = await GetVariablesNameAsync(UrlAseguradoraId, userData.CodeCompany, userData.Aseguradora),
                InsurancePlan = await GetVariablesNameAsync(UrlAseguradoraPlanId, userData.CodeCompany, userData.PlanAseguradora),
                MaritalState = await GetVariablesNameAsync(UrlMaritalStatusId, userData.CodeCompany, userData.MaritalStatus),
                PatientType = await GetVariablesNameAsync(UrlPatientTypeId, userData.CodeCompany, userData.PatientType)
            };
            return "Tipo de documento: "+citaModel.DocumentType + "\n\n  Numero de documento: " + userData.DocumentId+ "\n\n  Nombre paciente: " + userData.Name+ " \n\n  Apellido paciente: " + userData.LastName+ "\n\n  Correo electronico: " + userData.Email+ "\n\n Celular: " + userData.Phone+ "\n\n Fecha de nacimiento: "+userData.birthdate+ "\n\n Sexo: "+userData.Gender+ "\n\n Estado civil: "+citaModel.MaritalState+ "\n\n EPS: "+citaModel.Insurance+ "\n\n Plan: "+ citaModel.InsurancePlan + "\n\n Tipo de afiliacion: "+citaModel.AfiliationType+ "\n\n Tipo de paciente: "+citaModel.PatientType+ "\n\n Nombre ciudad: "+citaModel.City+ "\n\n Direccion: "+userData.Address;

        }

        public async Task<string> PostCreateUserAsync(UserProfileModel userProfile)
        {
            var url = "https://api-users-test-001.azurewebsites.net/Patient";
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await AutenticationAsync(userProfile.CodeCompany));
                    var parametros = new
                    {
                        DocumentType = new { Id = int.Parse(userProfile.DocumentType) },
                        Document = userProfile.DocumentId,
                        userProfile.Name,
                        userProfile.LastName,
                        InsuranceId = new { Id = int.Parse(userProfile.Aseguradora) },
                        InsurancePlan = new { Id = int.Parse(userProfile.PlanAseguradora) },
                        userProfile.Email,
                        BirthDate = userProfile.birthdate,
                        Genre = userProfile.Gender,
                        city = new { Id = int.Parse(userProfile.City) },
                        CellPhone = userProfile.Phone,
                        userProfile.Address,
                        PatientType = new { Id = int.Parse(userProfile.PatientType) },
                        AffiliationType = new { Id = int.Parse(userProfile.Affiliation) },
                        MaritalStatus = new { Id = int.Parse(userProfile.MaritalStatus) },
                    };
                    var jsonContent = JsonConvert.SerializeObject(parametros);
                    var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await client.PostAsync(url, httpContent);

                    if (response.IsSuccessStatusCode)
                    {
                        if (response.StatusCode.Equals("204"))
                        {
                            return null;
                        }
                        string jsonResponse = await response.Content.ReadAsStringAsync();
                        try
                        {
                            JObject jsonObject = JObject.Parse(jsonResponse);
                            string id = jsonObject["id"].ToString();
                            return id;
                        }
                        catch (Exception ex)
                        {
                            return "error";
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Error: {response.StatusCode}");
                        return "error";
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Excepción: {ex.Message}");
                    return "error";
                }
            }

        }

        public async Task<List<string>> GetPreparationAsync(UserProfileModel userData)
        {
            string url = UrlPreparation + userData.Servicios + "&isPublic=true";
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await AutenticationAsync(userData.CodeCompany));
                    HttpResponseMessage response = await client.GetAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        if (response.StatusCode.Equals("204"))
                        {
                            return null;
                        }
                        string jsonResponse = await response.Content.ReadAsStringAsync();
                        JArray jsonArray;
                        List<string> names = new List<string>();
                        jsonArray = JArray.Parse(jsonResponse);
                        if (jsonArray.Count == 1)
                        {
                            names.Add(jsonArray[0]["name"]?.ToString());
                            return names;
                        }else if(jsonArray.Count > 1)
                        {
                            foreach (JObject obj in jsonArray)
                            {

                                if (obj["name"] != null)
                                {
                                    string name = obj["name"].ToString();
                                    names.Add(name);
                                }
                            }
                            return names;
                        }
                        else
                        {
                            return null;
                        }
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

        public async Task<bool> PutReagendarCitaAsync(object obj, string clientCode, string citaId, string UserId)
        {
            var url = "https://api-appointments-test-001.azurewebsites.net/Appointments";
            bool ResultRef = false;
            var dateTimeString = obj.GetType().GetProperty("DateTime");
            var durationObj = obj.GetType().GetProperty("Duration");
            var DoctorObj = obj.GetType().GetProperty("UserId");
            var OfficeObj = obj.GetType().GetProperty("OfficeId");
            string OfficeString = OfficeObj?.GetValue(obj) as string;
            string DoctorString = DoctorObj?.GetValue(obj) as string;
            string durationString = durationObj?.GetValue(obj) as string;
            if (!int.TryParse(durationString, out int duration))
            {
                throw new ArgumentException("AppointmentTypeId no es un entero válido.");
            }
            if (!int.TryParse(DoctorString, out int DoctorId))
            {
                throw new ArgumentException("AppointmentTypeId no es un entero válido.");
            }
            if (!int.TryParse(OfficeString, out int OfficeId))
            {
                throw new ArgumentException("AppointmentTypeId no es un entero válido.");
            }
            if (!int.TryParse(UserId, out int userId))
            {
                throw new ArgumentException("AppointmentTypeId no es un entero válido.");
            }
            string DateTimeString = dateTimeString?.GetValue(obj) as string;
            DateTime dateTime = DateTime.Parse(DateTimeString);
            DateTime nuevaFecha = dateTime.AddMinutes(duration);
            string horaFormateada = nuevaFecha.ToString("HH:mm:ss");
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await AutenticationAsync(clientCode));
                    var parametros = new Dictionary<string, object>
                    {
                       { "AppointmentID", int.Parse(citaId) },
                       { "DateFrom", dateTime},
                        { "To", horaFormateada },
                        { "DoctorID", DoctorId },
                        {"OfficeID",  OfficeId},
                        {"Origin", 3 },
                        {"UserID", userId }

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
                    return false;
                }
            }

        }

        public async Task<bool> PostCreateListaEsperaAsync(UserProfileModel userProfile)
        {
            var url = "https://api-waitinglist-test-001.azurewebsites.net/PatientWaitingList";
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await AutenticationAsync(userProfile.CodeCompany));
                    var parametros = new
                    {
                        userID = int.Parse(userProfile.UserId),
                        planID = int.Parse(userProfile.PlanAseguradora),
                        entityID = int.Parse(userProfile.Aseguradora),
                        serviceID = int.Parse(userProfile.Servicios),
                        createdUserID = int.Parse(userProfile.UserId),
                        Document = userProfile.DocumentId,
                        documentTypeID = int.Parse(userProfile.DocumentType),
                        name = userProfile.Name,
                        lastName = userProfile.LastName,
                        email = userProfile.Email,
                        cellPhone = userProfile.Phone,
                        specialtyID = int.Parse(userProfile.especialidad),
                        modality = "Presencial",
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
                        return true;
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

        public async Task<List<JObject>> GetDoctorListAsync(string clientCode, string DoctorName)
        {
            string ServicesEndpoint = "https://api-users-test-001.azurewebsites.net/Doctor/GetByName?name=" + DoctorName;
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await AutenticationAsync(clientCode));
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

    }
    }
