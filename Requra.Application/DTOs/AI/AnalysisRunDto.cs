using Requra.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.AI
{
    public class AnalysisRunDto
    {
        public Guid Id { get; set; }

        public Guid ProjectId { get; set; }

        public AnalysisRunStatus Status { get; set; } = default!;

        public int? Progress { get; set; }
        public string? CurrentNode { get; set; } //should be enum later

        public string? CurrentNodeLabel { get; set; }

        public string? ErrorMessage { get; set; }

        //aiJobId will be added later "Inicates job id of the current agent"

        public List<Guid> DocumentIds { get; set; } = new();

        public Guid? MeetingId { get; set; }

        public DateTime? StartedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt
        {
            get; set;
        }
    }
}
