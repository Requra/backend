using Requra.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.Project.ProjectResults.Feedbacks
{
    public class UpdateStakeholderFeedbackStatusRequest
    {
        public Guid ProjectId { get; set; }
        public Guid FeedbackId { get; set; }
        public string UserId { get; set; } 
        public StakeholderFeedbackStatus Status { get; set; }
        public string? ResolutionNote { get; set; }
        public bool? IsRead { get; set; }
    }
    public class UpdateStakeholderFeedbackStatusAPIRequest
    {
        public StakeholderFeedbackStatus Status { get; set; }
        public string? ResolutionNote { get; set; }
        public bool? IsRead { get; set; }
    }
}
