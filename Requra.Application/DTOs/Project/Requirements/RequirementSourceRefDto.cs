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
        public double? QualityScore { get; set; }
        public List<string> QualityIssues { get; set; } = new();
        public List<string> QualityWarnings { get; set; } = new();

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

        public List<RequirementSourceDto> SourceRefs { get; set; } = new();
    }
}
