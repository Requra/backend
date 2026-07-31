using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Requra.Application.DTOs.AI
{
    public class ProcessResponseDto
    {
        [JsonPropertyName("job_id")]
        public string JobId { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("attempt_number")]
        public int AttemptNumber { get; set; }

        [JsonPropertyName("idempotent")]
        public bool Idempotent { get; set; }

        [JsonPropertyName("links")]
        public LinksDto Links { get; set; }
    }
}
