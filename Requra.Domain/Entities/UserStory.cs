using Requra.Domain.Enums;

namespace Requra.Domain.Entities
{
    public class UserStory
    {

        public Guid Id { get; private set; }
        public string SourceUserStoryId { get; private set; } = null!;


        public string Title { get; private set; } = null!;

        public string? Description { get; private set; }

        public List<string> AcceptanceCriteria { get; private set; }= new();

        public UserStoryStatus Status { get; private set; }

        public UserStoryPriority Priority { get; private set; }

        public Language? Language { get; private set; }

        public string CreatorId { get; private set; } = null!;

        public Guid RequirementId { get; private set; }
        public Guid ProjectId { get; private set; }

        public string? JiraTicket { get; private set; }

        public DateTime CreatedAt { get; private set; }

        public DateTime UpdatedAt { get; private set; }


        // Navigation
        public ApplicationUser Creator { get; private set; } = null!;
        public Requirement Requirement { get; private set; } = null!;
        public Project Project { get; private set; } = null!;   
        //public ICollection<Comment> Comments { get; private set; } = new List<Comment>();

        // Constructor
        private UserStory()
        {

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


        public void UpdateDetails(string title, string? description, List<string>? acceptanceCriteria, Language? language)
        {
            Title = title;
            Description = description;
            AcceptanceCriteria = acceptanceCriteria ?? new List<string>();
            Language = language;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateDetails(string? title = null, string? description = null, List<string>? acceptanceCriteria = null, 
            UserStoryPriority? priority = null, UserStoryStatus? status = null, Language? language = null)
        {
            if (!string.IsNullOrWhiteSpace(title))
                Title = title;
            if (!string.IsNullOrWhiteSpace(description))
                Description = description;
            if (acceptanceCriteria != null && acceptanceCriteria.Any())
                AcceptanceCriteria = acceptanceCriteria;
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
    }
}