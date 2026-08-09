using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Requra.Application.DTOs.AI
{
    public class UserStorySourceRefDto
    {
        [JsonPropertyName("chunk_id")]
        public string ChunkId { get; set; }

        [JsonPropertyName("confidence_score")]
        public double ConfidenceScore { get; set; }

        [JsonPropertyName("document_name")]
        public string DocumentName { get; set; }

        [JsonPropertyName("page")]
        public object? Page { get; set; } // stays flexible since TS says null

        [JsonPropertyName("quote")]
        public string Quote { get; set; }

        [JsonPropertyName("source_id")]
        public string SourceId { get; set; }

        [JsonPropertyName("source_type")]
        public string SourceType { get; set; }

        // Matches: [property: string]: any;
        [JsonExtensionData]
        public Dictionary<string, object>? AdditionalProperties { get; set; }
    }
}
