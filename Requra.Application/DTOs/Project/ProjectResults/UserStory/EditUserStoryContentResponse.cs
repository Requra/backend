using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.Project.ProjectResults.UserStory
{
    public class AcceptanceCriterionDto
    {
        public Guid Id { get; set; }
        public string Text { get; set; } = null!;
        public string? Format { get; set; }
    }

    public class SourceRefDto
    {
        public object? Page { get; set; }
        public string? Quote { get; set; }
        public string? ChunkId { get; set; }
        public string? SourceId { get; set; }
        public string? SourceType { get; set; }
        public string? DocumentName { get; set; }
        public double ConfidenceScore { get; set; }
    }

    public class QualityDto
    {
        public double Score { get; set; }
        public List<string> Issues { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public string QualityStatus { get; set; } = "NOT_EVALUATED";
    }

    public class EditUserStoryContentResponse
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public string? UserStoryText { get; set; }
        public List<AcceptanceCriterionDto> AcceptanceCriteria { get; set; } = new();
        public string Priority { get; set; } = null!;
        public List<string> Labels { get; set; } = new();
        public Guid RequirementId { get; set; }
        public List<SourceRefDto> SourceRefs { get; set; } = new();
        public QualityDto? Quality { get; set; }
        public string WorkflowStatus { get; set; } = null!;
        public string? ReviewFeedback { get; set; }
        public string? ReviewedBy { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? LastModifiedBy { get; set; }
        public int Version { get; set; }
        public int RevisionNumber { get; set; }
        public string RevisionSource { get; set; } = null!;
    }
}
