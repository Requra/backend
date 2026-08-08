using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Requra.Application.DTOs.AI
{
    public class ExcelRowDto
    {
        [JsonPropertyName("acceptance_criteria")]
        public string AcceptanceCriteria { get; set; }

        [JsonPropertyName("actor")]
        public string Actor { get; set; }

        [JsonPropertyName("confidence")]
        public double Confidence { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("labels")]
        public string Labels { get; set; }

        [JsonPropertyName("priority")]
        public string Priority { get; set; }

        [JsonPropertyName("quality_issues")]
        public string QualityIssues { get; set; }

        [JsonPropertyName("quality_score")]
        public double QualityScore { get; set; }

        [JsonPropertyName("requirement_id")]
        public string RequirementId { get; set; }

        [JsonPropertyName("source_quotes")]
        public string SourceQuotes { get; set; }

        [JsonPropertyName("source_refs")]
        public List<RowSourceRefDto> SourceRefs { get; set; }

        [JsonPropertyName("source_requirement_id")]
        public string SourceRequirementId { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("user_story")]
        public string UserStory { get; set; }

        [JsonExtensionData]
        public Dictionary<string, object>? AdditionalProperties { get; set; }

    }
}
