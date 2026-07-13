using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Requra.Application.DTOs.AI
{
    //public class RequirementDto
    //{
    //    public string Id { get; set; }
    //    public string Title { get; set; }
    //    public string Description { get; set; }
    //    public string Type { get; set; }
    //    public string Priority { get; set; }
    //    public double ConfidenceScore { get; set; }
    //    public List<string> SourceDocumentIds { get; set; }
    //}
    public class RequirementDto
    {
        //[JsonPropertyName("id")]
        //public int Id { get; set; }

        //[JsonPropertyName("text")]
        //public string Text { get; set; }

        //[JsonPropertyName("actor")]
        //public string Actor { get; set; }

        //[JsonPropertyName("goal")]
        //public string Goal { get; set; }

        //[JsonPropertyName("candidate_labels")]
        //public List<string> CandidateLabels { get; set; }

        //[JsonPropertyName("confidence")]
        //public double Confidence { get; set; }

        //[JsonPropertyName("evidence")]
        //public List<EvidenceReferenceDto> Evidence { get; set; }

        //[JsonPropertyName("needs_review")]
        //public bool NeedsReview { get; set; }

        //[JsonPropertyName("review_reason")]
        //public string ReviewReason { get; set; }

        //[JsonPropertyName("labels")]
        //public List<string> Labels { get; set; }

        //[JsonPropertyName("classification_confidence")]
        //public double ClassificationConfidence { get; set; }
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("category")]
        public string Category { get; set; }

        [JsonPropertyName("priority")]
        public string Priority { get; set; }

        [JsonPropertyName("actor")]
        public string Actor { get; set; }

        [JsonPropertyName("confidence_score")]
        public double ConfidenceScore { get; set; }

        [JsonPropertyName("deduplication_key")]
        public string DeduplicationKey { get; set; }

        [JsonPropertyName("source_refs")]
        public List<SourceRefDto> SourceRefs { get; set; }

        [JsonPropertyName("quality")]
        public QualityDto Quality { get; set; }
    }
}
