using Requra.Domain.Enums;

namespace Requra.Domain.Entities
{
    public class Project
    {
        public Guid Id { get; private set; }

        //public Guid OwnerId { get; private set; }

        public string Name { get; private set; } = null!;

        public string? Description { get; private set; }

        public Language Language { get; private set; }

        public ProjectStatus Status { get; private set; }

        public DateTime CreatedAt { get; private set; }

        public DateTime UpdatedAt { get; private set; }

        public ProjectType ProjectType { get; private set; }
        public bool IsDeleted { get; private set; }


        // Navigation
        //public ApplicationUser Owner { get; private set; } = null!;
        public ICollection<Document> Documents { get; private set; } = new List<Document>();

        public ICollection<ProjectMember> Members { get; private set; } = new List<ProjectMember>();
        public ICollection<UserStory> UserStories { get; private set; } = new List<UserStory>();


        private Project()
        {
            
        }
        public Project( string name, string? description, ProjectType projectType, Language language = Language.En)
        {
            Id = Guid.NewGuid();
            Name = name;
            Description = description;
            Language = language;
            ProjectType = projectType;
            Status = ProjectStatus.InProgress;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
      
        //public void UpdateDetails(string name, string? description, Language language)
        //{
        //    Name = name;
        //    Description = description;
        //    Language = language;
        //    UpdatedAt = DateTime.UtcNow;
        //}

        public void UpdateDetails(string? name, string? description,ProjectType? projectType, ProjectStatus? projectStatus , Language? language)
        {
            Name = name?? Name;
            Description = description ?? Description;
            Language = language ?? Language;
            ProjectType = projectType ?? ProjectType;
            Status = projectStatus ?? Status;
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
        public void Delete()
        {
            IsDeleted = true;
            UpdatedAt = DateTime.UtcNow;
        }
        //public void SetProjectType(ProjectType type)
        //{
        //    ProjectType = type;
        //}
        public void AddMember(string userId, ProjectRole role)
        {
            if (Members.Any(m => m.UserId == userId))
                return;

            Members.Add(new ProjectMember(userId, Id, role));
        }
    }
}