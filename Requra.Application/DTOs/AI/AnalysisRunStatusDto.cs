using Requra.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.AI
{
    public class AnalysisRunStatusDto
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public AnalysisRunStatus Status { get; set; }
        public int? Progress { get; set; }
        public string Messsage { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

}
}
