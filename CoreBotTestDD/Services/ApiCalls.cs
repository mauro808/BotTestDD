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
            string ServicesEndpoint = "https://api-locations-prod-001.azurewebsites.net/Cities/GetByName?name="+ciudad;
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
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await AutenticationAsync(userProfile.CodeCompany));
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
                        bool responseRef = await PostRefreshCitaAsync(JObject.Parse(jsonResponse), userProfile.CodeCompany);
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

        public async Task<string> GetPreparationAsync(UserProfileModel userData)
        {
            string url = UrlPreparation + userData.Servicios + "&isPublic=false";
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
                        jsonArray = JArray.Parse(jsonResponse);
                        if (jsonArray.Count > 0)
                        {
                            return jsonArray[0]["name"]?.ToString();
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

    }
    }
