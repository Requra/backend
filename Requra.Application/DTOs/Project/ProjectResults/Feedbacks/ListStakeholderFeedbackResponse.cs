using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.Project.ProjectResults.Feedbacks
{
    public class ListStakeholderFeedbackResponse
    {
        public List<SubmitStakeholderFeedbackResponse> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int OpenCount { get; set; }
        public int ResolvedCount { get; set; }
        public int UnreadCount { get; set; }
    }
}
