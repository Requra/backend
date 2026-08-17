using Requra.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.Project.Requirements
{
    public class RequirementSourceDto
    {
        public int? Page { get; set; }
        public string? Quote { get; set; }
        public string? ChunkId { get; set; }
        public string? SourceId { get; set; }
        public string? SourceType { get; set; }
        public string? DocumentName { get; set; }
        public double? ConfidenceScore { get; set; }
    }
    public class RequirementLinkedUserStoryDto
    {
        public Guid Id { get; set; }
        public string? SourceUserStoryId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
    public class RequirementsDto
    {
        public Guid Id { get; set; }
        public string SourceRequirementId { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Language { get; set; }

        public Guid? ProjectId { get; set; }

        public double? ConfidenceScore { get; set; }
        //public double? QualityScore { get; set; }
        //public List<string> QualityIssues { get; set; } = new();
        //public List<string> QualityWarnings { get; set; } = new();
        public QualityDto Quality { get; set; } = new();

        public string? DeduplicationKey { get; set; }
        public string? Actor { get; set; }
        public string? Category { get; set; }
        public string? Priority { get; set; }

        public string? ReviewedById { get; set; }
        public string? ReviewFeedback { get; set; }
        public DateTime? ReviewedAt { get; set; }

        public string? LastModifiedById { get; set; }
        public int? Version { get; set; }
        public QualityStatus? QualityStatus { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public List<RequirementSourceDto>? SourceRefs { get; set; } = new();
        
        public string? ReviewedBy { get; set; }
        public string? LastModifiedBy { get; set; }


        public int? LinkedUserStoryCount { get; set; }
        public int? ApprovedUserStoryCount { get; set; }
        public int? StoryCoveragePercent { get; set; }

        public List<RequirementLinkedUserStoryDto>? LinkedUserStories { get; set; } = new();
    }

    public class GetProjectRequirementsRequest
    {
        public Guid ProjectId { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 25;
        public List<string>? Status { get; set; }
        public string? Search { get; set; }
    }

    public class QualityDto
    {
        public double? Score { get; set; }
        public List<string> Issues { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }
}
