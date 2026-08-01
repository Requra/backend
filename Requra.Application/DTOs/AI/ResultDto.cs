using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Requra.Application.DTOs.AI
{
    public class ResultDto
    {
        [JsonPropertyName("contract_version")]
        public string ContractVersion { get; set; }

        [JsonPropertyName("job_id")]
        public string JobId { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("is_useful")]
        public bool IsUseful { get; set; }

        [JsonPropertyName("relevance_score")]
        public double RelevanceScore { get; set; }

        [JsonPropertyName("source_documents")]
        public List<SourceDocumentDto> SourceDocuments { get; set; }

        [JsonPropertyName("requirements")]
        public List<RequirementDto> Requirements { get; set; }

        [JsonPropertyName("user_stories")]
        public List<UserStoryDto> UserStories { get; set; }

        [JsonPropertyName("requirement_coverages")]
        public List<RequirementCoverageDto> RequirementCoverages { get; set; }

        [JsonPropertyName("summary")]
        public SummaryDto Summary { get; set; }

        [JsonPropertyName("exports")]
        public ExportsDto Exports { get; set; }

        [JsonPropertyName("artifacts")]
        public ArtifactsDto Artifacts { get; set; }

        [JsonPropertyName("quality_issues")]
        public List<string> QualityIssues { get; set; }

        [JsonPropertyName("warnings")]
        public List<WarningDto> Warnings { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }

        [JsonPropertyName("processing_time_ms")]
        public int ProcessingTimeMs { get; set; }

        [JsonPropertyName("error_message")]
        public string? ErrorMessage { get; set; }

        [JsonPropertyName("export_rows")]
        public List<object> ExportRows { get; set; }


}

}
