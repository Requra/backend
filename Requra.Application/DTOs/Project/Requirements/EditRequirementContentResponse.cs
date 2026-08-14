using Requra.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.Project.Requirements
{
    public class EditRequirementContentResponse
    {
        public string Id { get; set; } = null!;
        public Guid ProjectId { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public string Type { get; set; } = null!;
        public string? Priority { get; set; }
        public double? ConfidenceScore { get; set; }
        public List<Guid> SourceDocumentIds { get; set; } = new();
        public List<RequirementSourceRefDto> SourceRefs { get; set; } = new();
        public RequirementQualityDto Quality { get; set; } = new();
        public QualityStatus? QualityStatus { get; set; } 
        public string WorkflowStatus { get; set; } = null!;
        public string? ReviewFeedback { get; set; }
        public string? ReviewedBy { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? LastModifiedBy { get; set; }
        public int? Version { get; set; }
    }

    public class RequirementSourceRefDto
    {
        public string? SourceDocumentId { get; set; }
        public Guid? BackendDocumentId { get; set; }
        public int? Page { get; set; }
        public string? ChunkId { get; set; }
        public string? Quote { get; set; }
    }
    public class RequirementQualityDto
    {
        public double? Score { get; set; }
        public List<string> Issues { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }
}
