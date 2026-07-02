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
        public string ExecutiveSummary { get; set; }

        [JsonPropertyName("scope")]
        public List<string> Scope { get; set; }
    }
}
