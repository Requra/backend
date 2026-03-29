using Requra.Domain.Enums;

namespace Requra.Domain.Entities
{
    public class ProjectMember
    {
        public string UserId { get; private set; } = null!;

        public Guid ProjectId { get; private set; }

        public ProjectRole Role { get; private set; } 

        public DateTime JoinedAt { get; private set; }

        // Navigation
        public ApplicationUser User { get; private set; } = null!;
        public Project Project { get; private set; } = null!;

        // Constructor

        private ProjectMember()
        {
            
        }
        public ProjectMember(string userId, Guid projectId, ProjectRole role)
        {
            UserId = userId;
            ProjectId = projectId;
            Role = role;
            JoinedAt = DateTime.UtcNow;
        }

        public void ChangeRole(ProjectRole role)
        {
            Role = role;
        }
    }
}