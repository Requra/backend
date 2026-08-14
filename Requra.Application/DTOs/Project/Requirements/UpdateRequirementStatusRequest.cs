using Requra.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.Project.Requirements
{
    public class UpdateRequirementStatusRequest
    {
        public Guid ProjectId { get; set; }
        public Guid RequirementId { get; set; }
        public RequirementStatus WorkflowStatus { get; set; }
        public string? ReviewFeedback { get; set; }
        public string? ReviewedById { get; set; }
        public string? IfMatch { get; set; }


    }
}
