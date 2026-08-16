using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.Project.ProjectResults.UserStory
{
    public class UpdateUserStoryStatusResponse
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public string WorkflowStatus { get; set; } = null!;
        public string? ReviewFeedback { get; set; }
        public string? ReviewedBy { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int? Version { get; set; }
    }
}
