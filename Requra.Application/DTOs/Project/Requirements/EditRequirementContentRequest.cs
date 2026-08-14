using Requra.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.Project.Requirements
{
    public class EditRequirementContentRequest
    {
        public Guid ProjectId { get; set; }
        public Guid RequirementId { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public RequirementType? Type { get; set; }
        public string? Priority { get; set; }
        public string? IfMatch { get; set; }
        public string? CurrentUserId { get; set; }
    }
}
