using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Requra.Application.DTOs.AI
{
    public class JobResultResponseDto
    {
        [JsonPropertyName("error")]
        public string Error { get; set; }

        [JsonPropertyName("job_id")]
        public string JobId { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("exports")]
        public ExportsDto Exports { get; set; }
    }
}
