using Requra.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Requra.Application.DTOs.AI
{


    public class ProcessJsonResponse
    {
        [JsonPropertyName("job_id")]
        public Guid JobId { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }
        //[JsonPropertyName("contract_version")]
        //public string ContractVersion { get; set; } = string.Empty;

        //[JsonPropertyName("job_id")]
        //public Guid JobId { get; set; } 

        //[JsonPropertyName("status")]
        //public AnalysisRunStatus Status { get; set; } 

        //[JsonPropertyName("requirements")]
        //public List<RequirementDto> Requirements { get; set; } = new();

        //[JsonPropertyName("user_stories")]
        //public List<UserStoryDto> UserStories { get; set; } = new();

        //[JsonPropertyName("summary")]
        //public SummaryDto Summary { get; set; } = new();

        //[JsonPropertyName("risks")]
        //public List<RiskDto> Risks { get; set; } = new();

        //[JsonPropertyName("open_questions")]
        //public List<OpenQuestionDto> OpenQuestions { get; set; } = new();

        //[JsonPropertyName("action_items")]
        //public List<ActionItemDto> ActionItems { get; set; } = new();

        //[JsonPropertyName("warnings")]
        //public List<string> Warnings { get; set; } = new();

        //[JsonPropertyName("error")]
        //public string? Error { get; set; }
        //---------------------------------------------------
        //    [JsonPropertyName("job_id")]
        //    public Guid JobId { get; set; }

        //    [JsonPropertyName("status")]
        //    public string Status { get; set; }

        //    [JsonPropertyName("is_useful")]
        //    public bool IsUseful { get; set; }

        //    [JsonPropertyName("relevance_score")]
        //    public double RelevanceScore { get; set; }

        //    [JsonPropertyName("user_stories")]
        //    public List<UserStoryDto> UserStories { get; set; }= new List<UserStoryDto>();

        //    [JsonPropertyName("requirements")]
        //    public List<RequirementDto> Requirements { get; set; }=new List<RequirementDto>();

        //    [JsonPropertyName("requirement_coverages")]
        //    public List<RequirementCoverageDto> RequirementCoverages { get; set; } = new List<RequirementCoverageDto>();

        //    [JsonPropertyName("summary")]
        //    public SummaryDto Summary { get; set; }

        //    [JsonPropertyName("export_rows")]
        //    public List<object> ExportRows { get; set; }

        //    [JsonPropertyName("quality_issues")]
        //    public List<object> QualityIssues { get; set; }

        //    [JsonPropertyName("warnings")]
        //    public List<string> Warnings { get; set; }

        //    [JsonPropertyName("error_message")]
        //    public string ErrorMessage { get; set; }

        //    [JsonPropertyName("processing_time_ms")]
        //    public int ProcessingTimeMs { get; set; }
        //}
    }
}
