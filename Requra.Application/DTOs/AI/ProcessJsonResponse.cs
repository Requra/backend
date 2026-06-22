using Requra.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Requra.Application.DTOs.AI
{


    public class ProcessJsonResponse
    {
        [JsonPropertyName("contract_version")]
        public string ContractVersion { get; set; } = string.Empty;

        [JsonPropertyName("job_id")]
        public string JobId { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public AnalysisRunStatus Status { get; set; } 

        [JsonPropertyName("requirements")]
        public List<RequirementDto> Requirements { get; set; } = new();

        [JsonPropertyName("user_stories")]
        public List<UserStoryDto> UserStories { get; set; } = new();

        [JsonPropertyName("summary")]
        public SummaryDto Summary { get; set; } = new();

        [JsonPropertyName("risks")]
        public List<RiskDto> Risks { get; set; } = new();

        [JsonPropertyName("open_questions")]
        public List<OpenQuestionDto> OpenQuestions { get; set; } = new();

        [JsonPropertyName("action_items")]
        public List<ActionItemDto> ActionItems { get; set; } = new();

        [JsonPropertyName("warnings")]
        public List<string> Warnings { get; set; } = new();

        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }
}
