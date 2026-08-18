using Requra.Domain.Enums;

namespace Requra.Domain.Entities
{
    public class UserStory
    {
        public Guid Id { get; private set; }

        public string? SourceUserStoryId { get; private set; } = null!;

        public string Title { get; private set; } = null!;

        public string? Description { get; private set; }

        public UserStoryType Type { get; private set; }

        public UserStoryStatus Status { get; private set; }

        public UserStoryPriority Priority { get; private set; }

        public Language? Language { get; private set; }

        public int? StoryPoints { get; private set; }

        public string? DeduplicationKey { get; private set; }

        // The source requirement ID coming from AI: REQ-001
        public string? SourceRequirementId { get; private set; }

        public string? CreatorId { get; private set; } 

        public Guid RequirementId { get; private set; }

        public Guid ProjectId { get; private set; }

        public string? JiraTicket { get; private set; }

        public DateTime CreatedAt { get; private set; }

        public DateTime UpdatedAt { get; private set; }
        // update 
        public string? ReviewFeedback { get; private set; }
        public string? ReviewedById { get; private set; }
        public DateTime? ReviewedAt { get; private set; }
        public int Version { get; private set; } = 1;
        public string? LastModifiedBy { get; private set; }
        public int RevisionNumber { get; private set; } = 1;
        public RevisionSource RevisionSource { get; private set; } = RevisionSource.AI_GENERATED;

        // Navigation
        public ApplicationUser Creator { get; private set; } = null!;

        public Requirement Requirement { get; private set; } = null!;

        public Project Project { get; private set; } = null!;


        // Child collections
        public List<AcceptanceCriterion> AcceptanceCriteria { get; private set; } = new();
        public List<string> Labels { get; private set; } = new();
        public List<UserStorySourceRef> SourceRefs { get; private set; } = new();

        public UserStoryQuality? Quality { get; private set; }

        public JiraFields? JiraFields { get; private set; }

        


        //public ICollection<Comment> Comments { get; private set; } = new List<Comment>();
    
        // Constructor
        private UserStory()
        {

        }
        public UserStory(
    string sourceUserStoryId,
    string title,
    string? description,
    List<AcceptanceCriterion> acceptanceCriteria,
    UserStoryType type,
    UserStoryStatus status,
    UserStoryPriority priority,
    Language? language,
    string? creatorId ,
    Guid requirementId,
    Guid projectId,
    int? storyPoints,
    string? sourceRequirementId,
    string? deduplicationKey)
        {
            Id = Guid.NewGuid();

            SourceUserStoryId = sourceUserStoryId;
            Title = title;
            Description = description;
            AcceptanceCriteria = acceptanceCriteria;

            Type = type;
            Status = status;
            Priority = priority;
            Language = language;

            CreatorId = creatorId;
            RequirementId = requirementId;
            ProjectId = projectId;

            StoryPoints = storyPoints;
            SourceRequirementId = sourceRequirementId;
            DeduplicationKey = deduplicationKey;

            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
        public UserStory(string title, string creatorId, Guid requirementId, UserStoryPriority priority, Guid? projectId = null)
        {
            Id = Guid.NewGuid();
            Title = title;
            CreatorId = creatorId;
            RequirementId = requirementId;
            Priority = priority;
            ProjectId = projectId ?? Guid.Empty;

            Status = UserStoryStatus.NeedReview;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }


        public void UpdateDetails(string title, string? description, Language? language)
        {
            Title = title;
            Description = description;
            //AcceptanceCriteria = acceptanceCriteria ?? new List<string>();
            Language = language;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateDetails(string? title = null, string? description = null,  
            UserStoryPriority? priority = null, UserStoryStatus? status = null, Language? language = null)
        {
            if (!string.IsNullOrWhiteSpace(title))
                Title = title;
            if (!string.IsNullOrWhiteSpace(description))
                Description = description;
            //if (acceptanceCriteria != null && acceptanceCriteria.Any())
            //    AcceptanceCriteria = acceptanceCriteria;
            if (priority.HasValue)
                Priority = priority.Value;
            if (status.HasValue)
                Status = status.Value;
            if (language.HasValue)
                Language = language;
            UpdatedAt = DateTime.UtcNow;
        }

        public void ChangeStatus(UserStoryStatus status)
        {
            Status = status;
            UpdatedAt = DateTime.UtcNow;
        }

        public void AddSourceReference(UserStorySourceRef reference)
        {
            SourceRefs.Add(reference);
        }

        public void ChangePriority(UserStoryPriority priority)
        {
            Priority = priority;
            UpdatedAt = DateTime.UtcNow;
        }

        public void LinkJira(string jiraTicket)
        {
            JiraTicket = jiraTicket;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetClickUpTaskId(string taskId)
        {
            JiraTicket = taskId;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetDescription(string? description)
        {
            Description = description;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetProjectId(Guid projectId)
        {
            ProjectId = projectId;
            UpdatedAt = DateTime.UtcNow;
        }
        public void Approve(string? reviewedById, string? reviewFeedback)
        {
            Status = UserStoryStatus.Approved;
            ReviewedById = reviewedById;
            ReviewFeedback = reviewFeedback;
            ReviewedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            Version += 1;
        }

        public void Reject(string? reviewedById, string? reviewFeedback)
        {
            Status = UserStoryStatus.Rejected;
            ReviewedById = reviewedById;
            ReviewFeedback = reviewFeedback;
            ReviewedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            Version += 1;
        }

        public void FlagForReview(string? reviewedById, string? reviewFeedback)
        {
            Status = UserStoryStatus.NeedReview;
            ReviewedById = reviewedById;
            ReviewFeedback = reviewFeedback;
            ReviewedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            Version += 1;
        }
        public void EditContent(
            string? title,
            string? description,
            List<AcceptanceCriterion>? acceptanceCriteria,
            UserStoryPriority? priority,
            List<string>? labels,
            string? modifiedById)
        {
            if (!string.IsNullOrWhiteSpace(title))
                Title = title;

            if (description != null)
                Description = description;

            if (acceptanceCriteria != null)
            {
                AcceptanceCriteria.Clear();
                AcceptanceCriteria.AddRange(acceptanceCriteria);
            }

            if (priority.HasValue)
                Priority = priority.Value;

            if (labels != null)
            {
                Labels.Clear();
                Labels.AddRange(labels);
            }

            Status = UserStoryStatus.Edited;
            LastModifiedBy = modifiedById;
            RevisionSource = RevisionSource.HUMAN_EDITED;
            RevisionNumber += 1;
            Version += 1;
            UpdatedAt = DateTime.UtcNow;

            Quality?.MarkStale();
        }
        public void AttachQuality(double? score, List<string>? issues, List<string>? warnings)
        {
            if (!score.HasValue)
                return;

            Quality = new UserStoryQuality(score.Value, issues, warnings);
        }
        public void RegenerateContent(
           string title,
           string? description,
           List<AcceptanceCriterion> acceptanceCriteria,
           List<string>? labels,
           string feedback,
           string? modifiedById)
        {
            Title = title;
            Description = description;

            AcceptanceCriteria.Clear();
            AcceptanceCriteria.AddRange(acceptanceCriteria);

            if (labels != null)
            {
                Labels.Clear();
                Labels.AddRange(labels);
            }

            Status = UserStoryStatus.NeedReview;
            ReviewFeedback = feedback;
            LastModifiedBy = modifiedById;
            RevisionSource = RevisionSource.AI_REGENERATED;
            RevisionNumber += 1;
            Version += 1;
            UpdatedAt = DateTime.UtcNow;

            Quality?.ResetToNotEvaluated();
        }
    }
}