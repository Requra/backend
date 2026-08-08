using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;


namespace Requra.Application.DTOs.AI
{

    public class WarningDto
    {
        [JsonPropertyName("code")]
        public string Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("node_name")]
        public string NodeName { get; set; }

        [JsonExtensionData]
        public Dictionary<string, object>? AdditionalProperties { get; set; }
    }
}
