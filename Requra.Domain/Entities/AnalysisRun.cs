using Requra.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Domain.Entities
{
    public class AnalysisRun
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }

        public AnalysisRunStatus Status { get; set; }
        public int? Progress { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        public string? ErrorMessage { get; set; }
    }
}
