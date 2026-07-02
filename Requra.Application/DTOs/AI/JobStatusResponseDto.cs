using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Requra.Application.DTOs.AI
{ 
    public class JobStatusResponseDto
    {
       [JsonPropertyName("status")]
            public string Status { get; set; }

            [JsonPropertyName("progress_pct")]
            public int ProgressPct { get; set; }

            [JsonPropertyName("current_node")]
            public string CurrentNode { get; set; }

            [JsonPropertyName("result")]
            public ResultDto? Result { get; set; }

            [JsonPropertyName("error")]
            public string? Error { get; set; }

        }
    }
   

