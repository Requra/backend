using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Requra.Application.DTOs.AI
{
    public class JiraRowDto
    {
        [JsonPropertyName("issue_type")]
        public string IssueType { get; set; }

        [JsonPropertyName("summary")]
        public string Summary { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("acceptance_criteria")]
        public List<string> AcceptanceCriteria { get; set; }

        [JsonPropertyName("priority")]
        public string Priority { get; set; }

        [JsonPropertyName("labels")]
        public List<string> Labels { get; set; }

        [JsonPropertyName("components")]
        public List<string> Components { get; set; }

        [JsonPropertyName("epic_name")]
        public string EpicName { get; set; }

        [JsonPropertyName("story_points")]
        public int StoryPoints { get; set; }

        [JsonPropertyName("source_requirement_id")]
        public string SourceRequirementId { get; set; }
    }
    }
