namespace CoreBotTestDD.Models
{
    public class TextAnalisysModel
    {
        public string kind { get; set; }
        public AnalysisInput analysisInput { get; set; }
        public Parameters parameters { get; set; }
    }

    public class Parameters
    {
        public string projectName { get; set; }
        public string deploymentName { get; set; }
        public string stringIndexType { get; set; }
    }

    public class AnalysisInput
    {
        public ConversationItem conversationItem { get; set; }
    }

    public class ConversationItem
    {
        public string id { get; set; }
        public string participantId { get; set; }
        public string text { get; set; }
    }

}
