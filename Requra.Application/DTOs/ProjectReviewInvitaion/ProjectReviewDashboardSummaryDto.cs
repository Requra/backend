using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.ProjectReviewInvitaion
{
    public class ProjectReviewDashboardSummaryDto
    {
        public string? ExecutiveSummary { get; set; }
        public List<string> KeyDecisions { get; set; } = new();
        public List<string> OpenQuestions { get; set; } = new();
        public List<string> Risks { get; set; } = new();
        public List<string> Assumptions { get; set; } = new();
        public List<string> Scope { get; set; } = new();
        public List<string> OutOfScope { get; set; } = new();
    }
}
