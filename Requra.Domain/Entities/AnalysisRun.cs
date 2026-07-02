using Microsoft.EntityFrameworkCore.Update.Internal;
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
        public DateTime? UpdatedAt { get; set; }


        public string? ErrorMessage { get; set; }

        public String? CurrentNode { get; set; } //Becomes an enum later
        public AnalysisRun() { }
        public void UpdateAnalysis(AnalysisRunStatus status,int? progress = 0,string? currentNode = null,string? errorMessage = null,DateTime? startedAt = null,DateTime? completedAt = null)
        {
            Status = status;
            Progress = progress ?? Progress;
            CurrentNode = currentNode ?? CurrentNode;
            ErrorMessage = errorMessage ?? ErrorMessage;
            StartedAt = startedAt ?? StartedAt;
            CompletedAt = completedAt ?? CompletedAt;
            UpdatedAt = DateTime.UtcNow;
        }
    }

}
