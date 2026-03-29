using Microsoft.AspNetCore.Identity;
using Requra.Domain.Enums;

namespace Requra.Domain.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; private set; }

        public Language? PreferredLanguage { get; private set; }

        public string? AvatarUrl { get; private set; }

        public bool IsActive { get; private set; }

        public DateTime? LastLoginAt { get; private set; }

        public DateTime CreatedAt { get; private set; }

        public DateTime UpdatedAt { get; private set; }
        public UserRole Role { get; set; }
        public virtual ICollection<RefreshToken>? RefreshTokens { get; set; } = new List<RefreshToken>();

        // Navigation properties
        public ICollection<Document> UploadedDocuments { get; private set; } = new List<Document>();
        public ICollection<ProjectMember> ProjectMemberships { get; private set; } = new List<ProjectMember>();
        public ICollection<MeetingParticipant> MeetingParticipations { get; private set; } = new List<MeetingParticipant>();
        public ICollection<UserStory> CreatedUserStories { get; private set; } = new List<UserStory>();
        public ICollection<Comment> Comments { get; private set; } = new List<Comment>();
        public ICollection<Approval> Approvals { get; private set; } = new List<Approval>();

        // Constructor
        public ApplicationUser(string userName, string email)
        {
            UserName = userName;
            Email = email;

            IsActive = true;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        

        public void UpdateProfile(string? fullName, Language? preferredLanguage, string? avatarUrl)
        {
            FullName = fullName;
            PreferredLanguage = preferredLanguage;
            AvatarUrl = avatarUrl;
            UpdatedAt = DateTime.UtcNow;
        }

        public void MarkLogin()
        {
            LastLoginAt = DateTime.UtcNow;
        }

        public void Deactivate()
        {
            IsActive = false;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Activate()
        {
            IsActive = true;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}