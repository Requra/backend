using Requra.Domain.Enums;

namespace Requra.Domain.Entities
{
    public class Project
    {
        public Guid Id { get; private set; }

        public Guid OwnerId { get; private set; }

        public string Name { get; private set; } = null!;

        public string? Description { get; private set; }

        public Language Language { get; private set; }

        public ProjectStatus Status { get; private set; }

        public DateTime CreatedAt { get; private set; }

        public DateTime UpdatedAt { get; private set; }

        // Navigation
        public ApplicationUser Owner { get; private set; } = null!;
        public ICollection<Document> Documents { get; private set; } = new List<Document>();

       
        public Project(Guid ownerId, string name, Language language = Language.En)
        {
            Id = Guid.NewGuid();
            OwnerId = ownerId;
            Name = name;
            Language = language;

            Status = ProjectStatus.InProgress;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        

        public void UpdateDetails(string name, string? description, Language language)
        {
            Name = name;
            Description = description;
            Language = language;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Draft()
        {
            Status = ProjectStatus.Drafted;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Completed()
        {
            Status = ProjectStatus.Completed;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Cancel()
        {
            Status = ProjectStatus.Cancelled;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}