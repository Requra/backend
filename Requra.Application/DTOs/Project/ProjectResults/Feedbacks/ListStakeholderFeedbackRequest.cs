using Requra.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.Project.ProjectResults.Feedbacks
{
    public class ListStakeholderFeedbackRequest
    {
        public StakeholderFeedbackStatus? Status { get; set; }
        public Guid ProjectId { get; set; } 
        public string? AuthorId { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class ListStakeholderFeedbackApiRequest
    {
        public StakeholderFeedbackStatus? Status { get; set; }
        public Guid ProjectId { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
