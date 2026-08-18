using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Requra.Application.DTOs.AI
{
    public class RegenerateStoryRequestDto
    {
        [JsonPropertyName("requirement_text")]
        public string RequirementText { get; set; } = null!;

        [JsonPropertyName("requirement_type")]
        public string? RequirementType { get; set; }

        [JsonPropertyName("actor")]
        public string? Actor { get; set; }

        [JsonPropertyName("priority")]
        public string? Priority { get; set; }

        [JsonPropertyName("feedback")]
        public string Feedback { get; set; } = null!;

        [JsonPropertyName("original_story")]
        public string? OriginalStory { get; set; }

        [JsonPropertyName("source_context")]
        public string? SourceContext { get; set; }
    }

    public class RegenerateAcceptanceCriterionDto
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; } = null!;

        [JsonPropertyName("criterion_type")]
        public string? CriterionType { get; set; }
    }

    public class RegenerateStoryResponseDto
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = null!;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("acceptance_criteria")]
        public List<RegenerateAcceptanceCriterionDto> AcceptanceCriteria { get; set; } = new();

        [JsonPropertyName("labels")]
        public List<string>? Labels { get; set; }
    }
}
