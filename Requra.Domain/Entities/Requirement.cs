using Requra.Domain.Enums;

namespace Requra.Domain.Entities
{
    public class Requirement
    {
        public Guid Id { get; private set; }

        public string Title { get; private set; } = null!;

        public string? Description { get; private set; }

        public RequirementType Type { get; private set; }

        public RequirementStatus Status { get; private set; }

        public Language? Language { get; private set; }

        public DateTime CreatedAt { get; private set; }

        public DateTime UpdatedAt { get; private set; }

        // Navigation
        public ICollection<DocumentRequirement> DocumentRequirements { get; private set; } = new List<DocumentRequirement>();
        public ICollection<UserStory> UserStories { get; private set; } = new List<UserStory>();
        public ICollection<Approval> Approvals { get; private set; } = new List<Approval>();

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

            Status = RequirementStatus.Drafted;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        // 🔥 Behavior Methods

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

        public void Approve()
        {
            Status = RequirementStatus.Approved;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Reject()
        {
            Status = RequirementStatus.Rejected;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}