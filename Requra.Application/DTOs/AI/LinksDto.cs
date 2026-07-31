using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Requra.Application.DTOs.AI
{
    public class LinksDto
    {
        [JsonPropertyName("self")]
        public string Self { get; set; }

        [JsonPropertyName("result")]
        public string Result { get; set; }

        [JsonPropertyName("cancel")]
        public string Cancel { get; set; }

        [JsonPropertyName("retry")]
        public string Retry { get; set; }
    }
}
