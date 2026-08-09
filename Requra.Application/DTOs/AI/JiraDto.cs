using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Requra.Application.DTOs.AI
{
    public class JiraDto
    {
        [JsonPropertyName("available")]
        public bool Available { get; set; }

        [JsonPropertyName("issue_type")]
        public string IssueType { get; set; }

        [JsonPropertyName("rows")]
        public List<JiraRowDto> Rows { get; set; }

        [JsonExtensionData]
        public Dictionary<string, object>? AdditionalProperties { get; set; }
    }

}
