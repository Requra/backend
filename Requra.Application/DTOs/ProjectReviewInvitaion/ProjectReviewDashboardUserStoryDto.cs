using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.ProjectReviewInvitaion
{
    public class ProjectReviewDashboardUserStoryDto
    {
        public string Id { get; set; } = null!;
        public Guid FeedbackTargetId { get; set; }                  // real UserStory.Id
        public string StoryId { get; set; } = null!;
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? UserStory { get; set; }
        public List<string> AcceptanceCriteria { get; set; } = new();
        public string? Priority { get; set; }
        public string? RequirementId { get; set; }
        public string? Classification { get; set; }
    }
}
