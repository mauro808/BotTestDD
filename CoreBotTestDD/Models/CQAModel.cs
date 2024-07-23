using System.Collections.Generic;

namespace CoreBotTestDD.Models
{
    public class CQAModel
    {
        public int top { get; set; }
        public string question { get; set; }
        public bool includeUnstructuredSources { get; set; }
        public string confidenceScoreThreshold { get; set; }
        public AnswerSpanRequest answerSpanRequest { get; set; }
        public Filters filters { get; set; }
    }

    public class AnswerSpanRequest
    {
        public bool enable { get; set; }
        public int topAnswersWithSpan { get; set; }
        public string confidenceScoreThreshold { get; set; }
    }

    public class MetadataFilter
    {
        public string logicalOperation { get; set; }
        public List<MetadataItem>? metadata { get; set; }
    }

    public class MetadataItem
    {
        public string key { get; set; }
        public string value { get; set; }
    }

    public class Filters
    {
        public MetadataFilter? metadataFilter { get; set; }
    }
}
