using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Requra.Application.DTOs.AI
{ 
    public class JobStatusResponseDto
    {
            [JsonPropertyName("job_id")]
            public string JobId { get; set; }

            [JsonPropertyName("status")]
            public string Status { get; set; }

            [JsonPropertyName("progress_pct")]
            public int ProgressPct { get; set; }

            [JsonPropertyName("current_node")]
            public string CurrentNode { get; set; }

            [JsonPropertyName("error")]
            public string Error { get; set; }

            [JsonPropertyName("created_at")]
            public double CreatedAt { get; set; }

            [JsonPropertyName("updated_at")]
            public double UpdatedAt { get; set; }

            [JsonPropertyName("completed_at")]
            public double? CompletedAt { get; set; } 

            [JsonPropertyName("attempt_number")]
            public int AttemptNumber { get; set; }

            [JsonPropertyName("tenant_id")]
            public string TenantId { get; set; }

            [JsonPropertyName("project_id")]
            public string ProjectId { get; set; }

            [JsonPropertyName("input_type")]
            public string InputType { get; set; }

            [JsonPropertyName("error_code")]
            public string ErrorCode { get; set; }

            [JsonPropertyName("warning_count")]
            public int WarningCount { get; set; }

            [JsonPropertyName("quality_score")]
            public double? QualityScore { get; set; }

            [JsonPropertyName("links")]
            public LinksDto Links { get; set; }
       

    }
    }
   

