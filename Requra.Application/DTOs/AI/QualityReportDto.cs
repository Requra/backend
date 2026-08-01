using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Requra.Application.DTOs.AI
{
    public class QualityReportDto
    {
        [JsonPropertyName("acceptance_criteria_quality")]
        public double AcceptanceCriteriaQuality { get; set; }

        [JsonPropertyName("duplicate_risk")]
        public double DuplicateRisk { get; set; }

        [JsonPropertyName("groundedness_score")]
        public double GroundednessScore { get; set; }

        [JsonPropertyName("high_severity_issue_count")]
        public int HighSeverityIssueCount { get; set; }

        [JsonPropertyName("overall_score")]
        public double OverallScore { get; set; }

        [JsonPropertyName("requirement_count")]
        public int RequirementCount { get; set; }

        [JsonPropertyName("story_completeness")]
        public double StoryCompleteness { get; set; }

        [JsonPropertyName("story_count")]
        public int StoryCount { get; set; }

        [JsonPropertyName("traceability_coverage")]
        public double TraceabilityCoverage { get; set; }

        [JsonExtensionData]
        public Dictionary<string, object>? AdditionalProperties { get; set; }
    }
}
