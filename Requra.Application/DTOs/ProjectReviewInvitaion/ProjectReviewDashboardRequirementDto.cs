using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.ProjectReviewInvitaion
{
    public class ProjectReviewDashboardRequirementDto
    {
        public string Id { get; set; } = null!;
        public string RequirementId { get; set; } = null!;
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Classification { get; set; }
        public string? Priority { get; set; }
        public double ConfidenceScore { get; set; }
    }
}
