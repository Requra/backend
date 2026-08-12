using Requra.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.Project.ProjectResults.Feedbacks
{
    public class ListProjectStakeholderFeedbackRequest
    {
        public Guid ProjectId { get; set; }
        public string? UserId { get; set; }
        public StakeholderFeedbackStatus? Status { get; set; }
        public FeedbackTargetType? TargetType { get; set; }
        public bool? IsRead { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
