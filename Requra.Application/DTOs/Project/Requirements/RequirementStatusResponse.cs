using Requra.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.Project.Requirements
{
    public class UpdateRequirementStatusResponse
    {
        public string Id { get; set; } = null!;
        public Guid ProjectId { get; set; }
        public string WorkflowStatus { get; set; } = null!;
        public string? ReviewFeedback { get; set; }
        public string? ReviewedBy { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int? Version { get; set; }
    }
}
