using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Requra.Application.DTOs.AI
{
    public class RetryJobResponseDto
    {
        [JsonPropertyName("job_id")]
        public string JobId { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("attempt_number")]
        public int? AttemptNumber { get; set; }

        [JsonPropertyName("cancelled")]
        public bool? Cancelled { get; set; }

        [JsonPropertyName("detail")]
        public object Detail { get; set; } // Can be string or object with message and status properties
    }
}
