using Requra.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.Project.ProjectResults.Feedbacks
{
    public class SubmitStakeholderFeedbackRequest
    {
        public FeedbackTargetType TargetType { get; set; }
        public Guid TargetId { get; set; }
        public string Content { get; set; } = null!;
        public string CurrentUserId { get; set; }
    }
    public class SubmitStakeholderFeedbackApiRequest
    {
        public FeedbackTargetType TargetType { get; set; }
        public Guid TargetId { get; set; }
        public string Content { get; set; } = null!;
    }
}
