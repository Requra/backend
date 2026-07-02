using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Requra.Application.DTOs.AI
{
    //public class MetadataDto
    //{
    //    public Guid Project_Id { get; set; }

    //    public Guid? Meeting_Id { get; set; }

    //    public Guid Analysis_Run_Id { get; set; }
    //}
    public class MetadataDto
    {
        [JsonPropertyName("project_id")]
        public string? ProjectId { get; set; }

        [JsonPropertyName("meeting_id")]
        public string? MeetingId { get; set; }

        [JsonPropertyName("analysis_run_id")]
        public string? AnalysisRunId { get; set; }

        [JsonPropertyName("requested_by_user_id")]
        public string? RequestedByUserId { get; set; }
    }
}
