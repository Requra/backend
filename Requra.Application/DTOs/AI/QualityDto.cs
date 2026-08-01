using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Requra.Application.DTOs.AI
{
    public class QualityDto
    {
        [JsonPropertyName("score")]
        public double Score { get; set; }

        [JsonPropertyName("issues")]
        public List<string> Issues { get; set; }

        [JsonPropertyName("warnings")]
        public List<string> Warnings { get; set; }

    }
}
