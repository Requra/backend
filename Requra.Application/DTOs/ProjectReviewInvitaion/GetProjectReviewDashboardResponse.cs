using Requra.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.ProjectReviewInvitaion
{
    public class GetProjectReviewDashboardResponse
    {
        public Guid ProjectId { get; set; } 
        public string ProjectName { get; set; } = null!;
        public Guid AnalysisRunId { get; set; }
        public DateTime GeneratedAt { get; set; }
        public ProjectReviewPermission Permission { get; set; }
        public ProjectReviewDashboardSummaryDto Summary { get; set; } = new();
        public List<ProjectReviewDashboardRequirementDto> Requirements { get; set; } = new();
        public List<ProjectReviewDashboardUserStoryDto> UserStories { get; set; } = new();
    }
}
