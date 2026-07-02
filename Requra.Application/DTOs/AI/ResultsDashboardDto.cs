using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.AI
{
    public class ResultsDashboardDto
    {
        public string ProjectId { get; set; }
        public string AnalysisRunId { get; set; }
        public string Status { get; set; }

        public SummaryDto Summary { get; set; }
        public MetricsDto Metrics { get; set; }

        public List<RequirementDto> Requirements { get; set; }
        public List<UserStoryDto> UserStories { get; set; }
        public List<RiskDto> Risks { get; set; }
        public List<OpenQuestionDto> OpenQuestions { get; set; }
        public List<ActionItemDto> ActionItems { get; set; }

        public DateTime GeneratedAt { get; set; }
    }
}
