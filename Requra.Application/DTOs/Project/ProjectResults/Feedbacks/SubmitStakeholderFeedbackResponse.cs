using Requra.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.Project.ProjectResults.Feedbacks
{
    public class SubmitStakeholderFeedbackResponse
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public FeedbackTargetType TargetType { get; set; }
        public Guid TargetId { get; set; }
        public string? TargetTitle { get; set; }
        public string Content { get; set; } = null!;
        public StakeholderFeedbackStatus Status { get; set; }
        public bool IsRead { get; set; }
        public FeedbackAuthorDto? Author { get; set; }
        public string? ResolutionNote { get; set; }
        public string? ResolvedById { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class FeedbackAuthorDto
    {
        public string? DisplayName { get; set; }
        public string? Email { get; set; }
    }
}
