using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Requra.Application.DTOs.AI
{
    public class ProjectReviewDashboardRawResultDto
    {
        [JsonPropertyName("summary")]
        public ProjectReviewDashboardRawSummaryDto? Summary { get; set; }

        [JsonPropertyName("requirements")]
        public List<ProjectReviewDashboardRawRequirementDto> Requirements { get; set; } = new();

        [JsonPropertyName("user_stories")]
        public List<ProjectReviewDashboardRawUserStoryDto> UserStories { get; set; } = new();
    }
    public class ProjectReviewDashboardRawSummaryDto
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
    public class ProjectReviewDashboardRawRequirementDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = null!;

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("priority")]
        public string? Priority { get; set; }

        [JsonPropertyName("confidence_score")]
        public double ConfidenceScore { get; set; }
    }
    public class ProjectReviewDashboardRawUserStoryDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = null!;

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("priority")]
        public string? Priority { get; set; }

        [JsonPropertyName("user_story")]
        public string? UserStory { get; set; }

        [JsonPropertyName("requirement_id")]
        public string? RequirementId { get; set; }

        [JsonPropertyName("acceptance_criteria")]
        public List<ProjectReviewDashboardRawAcceptanceCriterionDto> AcceptanceCriteria { get; set; } = new();
    }
    public class ProjectReviewDashboardRawAcceptanceCriterionDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = null!;

        [JsonPropertyName("text")]
        public string Text { get; set; } = null!;

        [JsonPropertyName("criterion_type")]
        public string? CriterionType { get; set; }
    }
}
