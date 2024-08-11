using Microsoft.Bot.Builder;

namespace CoreBotTestDD.Models
{
    public class UserProfileModel
    {
        public string UserId { get; set; }

        public string Company { get; set; }

        public string CodeCompany { get; set; }

        public bool Choice { get; set; }

        public int CurrentPage { get; set; } = 0;

        public bool IsNewUser { get; set; }

        public bool SaveData { get; set; }
        public string Name { get; set; }

        public string LastName { get; set; }

        public string birthdate { get; set; }

        public string especialidad { get; set; }
        public string DocumentType { get; set; }

        public string Affiliation {  get; set; }

        public string City {  get; set; }

        public string Email { get; set; }   
        public string Gender { get; set; }

        public string Address { get; set; }
        public string Phone {  get; set; }

        public string PatientType { get; set; }

        public string DocumentId { get; set; }

        public string Aseguradora { get; set; }

        public string PlanAseguradora { get; set; }

        public string Servicios { get; set; }   

        public string MaritalStatus {  get; set; }

        public string Conversation { get; set; }
        public int Age { get; set; }

        public UserState State { get; set; }

        public ConversationState ConversationState { get; set; }
    }
}
