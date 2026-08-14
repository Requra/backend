using Requra.Domain.Enums;

namespace Requra.Domain.Entities
{
    public class Requirement
    {
        public Guid Id { get; private set; }
        // AI identifier
        public string SourceRequirementId { get; private set; } = null!;

        public string Title { get; private set; } = null!;

        public string? Description { get; private set; }

        public RequirementType Type { get; private set; }

        public RequirementStatus Status { get; private set; }

        public Language? Language { get; private set; }

        public DateTime CreatedAt { get; private set; }

        public DateTime UpdatedAt { get; private set; }
        public Guid? ProjectId { get; private set; }
        // AI metadata
        public double? ConfidenceScore { get; private set; }

        public double? QualityScore { get; private set; }

        public string? QualityIssues { get; private set; }
        public string? QualityWarnings { get; private set; }

        public string? DeduplicationKey { get; private set; }

        public string? Actor { get; private set; }

        public string? Category { get; private set; }

        public string? Priority { get; private set; }
        public string? ReviewedById { get; private set; }
        public string? ReviewFeedback { get; private set; }
        public DateTime? ReviewedAt { get; private set; }

        public string? LastModifiedById { get; private set; }
        public int? Version { get; private set; }
        public QualityStatus? QualityStatus { get; private set; } 


        // Navigation
        public ICollection<DocumentRequirement> DocumentRequirements { get; private set; } = new List<DocumentRequirement>();
        public ICollection<UserStory> UserStories { get; private set; } = new List<UserStory>();
        public Project? Project { get; private set; } 

        public ICollection<Approval> Approvals { get; private set; } = new List<Approval>();
        public ApplicationUser? ReviewdBy { get; private set; }

        public ICollection<RequirementSourceReference> RequirementSourceReferences { get; private set; } = new List<RequirementSourceReference>();

        // Constructor

        private Requirement()
        {
            
        }
        public Requirement(string title, RequirementType type, Language? language = null)
        {
            Id = Guid.NewGuid();
            Title = title;
            Type = type;
            Language = language;

            Status = RequirementStatus.Generated;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
        public Requirement(string sourceRequirementId,string title,string? description,RequirementType type,Guid projectId,double? confidenceScore,double? qualityScore,string? qualityIssues,string? qualityWarnings,string? deduplicationKey,string? actor,string? category,string? priority,Language? language = null)
        {
            Id = Guid.NewGuid();

            SourceRequirementId = sourceRequirementId;
            Title = title;
            Description = description;
            Type = type;
            Language = language;
            ProjectId = projectId;

            ConfidenceScore = confidenceScore;
            QualityScore = qualityScore;
            QualityIssues = qualityIssues;
            QualityWarnings = qualityWarnings;
            DeduplicationKey = deduplicationKey;
            Actor = actor;
            Category = category;
            Priority = priority;

            Status = RequirementStatus.Generated;

            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
        public void AddSourceReference(RequirementSourceReference sourceReference)
        {
            RequirementSourceReferences.Add(sourceReference);
            UpdatedAt = DateTime.UtcNow;
        }


        public void UpdateDetails(string title, string? description, RequirementType type, Language? language)
        {
            Title = title;
            Description = description;
            Type = type;
            Language = language;
            UpdatedAt = DateTime.UtcNow;
        }

        public void ChangeStatus(RequirementStatus status)
        {
            Status = status;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Approve(string? reviewedBy,string? reviewFeedback)
        {
            Status = RequirementStatus.Approved;
            ReviewedById = reviewedBy;
            ReviewFeedback = reviewFeedback;
            ReviewedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;

        }

        public void Reject(string? reviewedBy, string? reviewFeedback)
        {
            Status = RequirementStatus.Rejected;
            ReviewedById = reviewedBy;
            ReviewFeedback = reviewFeedback;
            ReviewedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
        public void FlagForReview(string? reviewedBy,string? reviewFeedback)
        {
            Status = RequirementStatus.NeedsReview;

            ReviewedById = reviewedBy;
            ReviewFeedback = reviewFeedback;
            ReviewedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public void EditContent(string? title,string? description,RequirementType? type,string? priority,string modifiedById)
        {
            if (!string.IsNullOrWhiteSpace(title))
                Title = title;

            if (description != null)
                Description = description;

            if (type.HasValue)
                Type = type.Value;

            if (priority != null)
                Priority = priority;

            Status = RequirementStatus.Edited;
            LastModifiedById = modifiedById;
            Version += 1;
            UpdatedAt = DateTime.UtcNow;
        }

    }
}