using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Requra.Application.DTOs.AI
{
    public class QualityIssueDto
    {
        [JsonPropertyName("details")]
        public string Details { get; set; }

        [JsonPropertyName("item_id")]
        public int ItemId { get; set; }

        [JsonPropertyName("item_type")]
        public string ItemType { get; set; }

        [JsonPropertyName("rule_violated")]
        public string RuleViolated { get; set; }

        [JsonPropertyName("severity")]
        public string Severity { get; set; }

        [JsonExtensionData]
        public Dictionary<string, object>? AdditionalProperties { get; set; }
    }
}
