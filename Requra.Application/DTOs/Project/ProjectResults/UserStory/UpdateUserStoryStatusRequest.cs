using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.Project.ProjectResults.UserStory
{
    public class UpdateUserStoryStatusRequest
    { /// later updatse hena
        public Guid ProjectId { get; set; }
        public Guid StoryId { get; set; }
        public string WorkflowStatus { get; set; } = string.Empty;
        public string? Feedback { get; set; }
        public string? ReviewedById { get; set; }
        public string? IfMatch { get; set; }
    }
}
