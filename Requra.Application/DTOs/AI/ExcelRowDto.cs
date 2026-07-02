using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Requra.Application.DTOs.AI
{
    public class ExcelRowDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("user_story")]
        public string UserStory { get; set; }

        [JsonPropertyName("acceptance_criteria")]
        public string AcceptanceCriteria { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("priority")]
        public string Priority { get; set; }

        [JsonPropertyName("actor")]
        public string Actor { get; set; }

        [JsonPropertyName("source_requirement_id")]
        public string SourceRequirementId { get; set; }

        // this comes as stringified JSON in your response
        [JsonPropertyName("source_refs")]
        public string SourceRefs { get; set; } //Will be edited later it is only now for testing the working flow

    }
}
