using Requra.Domain.Enums;

namespace Requra.Domain.Entities
{
    public class UserStory
    {
        public Guid Id { get; private set; }

        public string Title { get; private set; } = null!;

        public string? Description { get; private set; }

        public string? AcceptanceCriteria { get; private set; }

        public UserStoryStatus Status { get; private set; }

        public UserStoryPriority Priority { get; private set; }

        public Language? Language { get; private set; }

        public string CreatorId { get; private set; } = null!;

        public Guid RequirementId { get; private set; }

        public string? JiraTicket { get; private set; }

        public DateTime CreatedAt { get; private set; }

        public DateTime UpdatedAt { get; private set; }

        // Navigation
        public ApplicationUser Creator { get; private set; } = null!;
        public Requirement Requirement { get; private set; } = null!;
        public ICollection<Comment> Comments { get; private set; } = new List<Comment>();

        // Constructor
        public UserStory(string title, string creatorId, Guid requirementId, UserStoryPriority priority)
        {
            Id = Guid.NewGuid();
            Title = title;
            CreatorId = creatorId;
            RequirementId = requirementId;
            Priority = priority;

            Status = UserStoryStatus.Draft;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }


        public void UpdateDetails(string title, string? description, string? acceptanceCriteria, Language? language)
        {
            Title = title;
            Description = description;
            AcceptanceCriteria = acceptanceCriteria;
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
    }
}