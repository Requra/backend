using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Requra.Application.DTOs.AI
{
    public class RequirementCoverageDto
    {
        [JsonPropertyName("requirement_id")]//
        public string RequirementId { get; set; }

        [JsonPropertyName("coverage_type")]//
        public string CoverageType { get; set; }

        [JsonPropertyName("story_ids")]//
        public List<string> StoryIds { get; set; }

        [JsonPropertyName("acceptance_criteria_ids")]//
        public List<string> AcceptanceCriteriaIds { get; set; }

        [JsonPropertyName("reason")] //
        public string? Reason { get; set; }
        [JsonExtensionData]
        public Dictionary<string, object>? AdditionalProperties { get; set; }
    }
}
