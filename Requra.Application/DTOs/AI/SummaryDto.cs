using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Requra.Application.DTOs.AI
{
    //public class SummaryDto
    //{
    //    public string ExecutiveSummary { get; set; }
    //    public string Scope { get; set; }
    //    public List<string> MainActors { get; set; }
    //    public List<string> MainGoals { get; set; }
    //}
    public class SummaryDto
    {
        [JsonPropertyName("executive_summary")]
        public string? ExecutiveSummary { get; set; }

        [JsonPropertyName("key_decisions")]
        public List<string> KeyDecisions { get; set; } = new();

        [JsonPropertyName("open_questions")]
        public List<string> OpenQuestions { get; set; } = new();

        [JsonPropertyName("risks")]
        public List<string> Risks { get; set; } = new();

        [JsonPropertyName("assumptions")]
        public List<string> Assumptions { get; set; } = new();

        [JsonPropertyName("scope")]
        public List<string> Scope { get; set; } = new();

        [JsonPropertyName("out_of_scope")]
        public List<string> OutOfScope { get; set; } = new();
    }
}
