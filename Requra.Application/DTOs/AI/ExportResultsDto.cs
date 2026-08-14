using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Requra.Application.DTOs.AI
{
    public class ExportResultsDto
    {
        [JsonPropertyName("fileName")]
        public string FileName { get; set; }

        [JsonPropertyName("fileUrl")]
        public string FileUrl { get; set; }

        [JsonPropertyName("format")]
        public string Format { get; set; }

        [JsonPropertyName("expiresAt")]
        public DateTime ExpiresAt { get; set; }
    }
}
