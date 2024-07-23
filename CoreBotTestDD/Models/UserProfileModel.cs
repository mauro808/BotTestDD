using Microsoft.Bot.Builder;

namespace CoreBotTestDD.Models
{
    public class UserProfileModel
    {
        public string UserId { get; set; }

        public bool IsNewUser { get; set; }

        public bool SaveData { get; set; }
        public string Name { get; set; }

        public string especialidad { get; set; }
        public string DocumentType { get; set; }

        public string DocumentId { get; set; }

        public string Aseguradora { get; set; }

        public string PlanAseguradora { get; set; }

        public string Servicios { get; set; }   

        public string Conversation { get; set; }
        public int Age { get; set; }

        public UserState State { get; set; }

        public ConversationState ConversationState { get; set; }
    }
}
