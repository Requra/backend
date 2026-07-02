using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Requra.Application.DTOs.AI
{
    //public class UserStoryDto
    //{
    //    public string Id { get; set; }
    //    public string Title { get; set; }
    //    public string Description { get; set; }
    //    public List<string> AcceptanceCriteria { get; set; }
    //    public string Priority { get; set; }
    //    public string RequirementId { get; set; }
    //}
    public class UserStoryDto
    {
        //[JsonPropertyName("id")]
        //public string Id { get; set; }

        //[JsonPropertyName("title")]
        //public string Title { get; set; }

        //[JsonPropertyName("description")]
        //public string Description { get; set; }

        //[JsonPropertyName("acceptance_criteria")]
        //public List<AcceptanceCriteriaDto> AcceptanceCriteria { get; set; }

        //[JsonPropertyName("source_requirement_ids")]
        //public List<int> SourceRequirementIds { get; set; }

        //[JsonPropertyName("labels")]
        //public List<string> Labels { get; set; }

        //[JsonPropertyName("evidence_reference")]
        //public List<EvidenceReferenceDto> EvidenceReference { get; set; }

        //[JsonPropertyName("source_fr_id")]
        //public int? SourceFrId { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("requirement_id")]
        public string RequirementId { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("user_story")]
        public string UserStory { get; set; }

        [JsonPropertyName("acceptance_criteria")]
        public List<AcceptanceCriteriaDto> AcceptanceCriteria { get; set; }

        [JsonPropertyName("priority")]
        public string Priority { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("deduplication_key")]
        public string DeduplicationKey { get; set; }

        [JsonPropertyName("source_refs")]
        public List<SourceRefDto> SourceRefs { get; set; }

        [JsonPropertyName("quality")]
        public QualityDto Quality { get; set; }

        [JsonPropertyName("jira_fields")]
        public JiraFieldsDto JiraFields { get; set; }
    }
}
