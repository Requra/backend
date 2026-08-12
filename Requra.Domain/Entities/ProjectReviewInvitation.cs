using Requra.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Domain.Entities
{
    public class ProjectReviewInvitation
    {
        public Guid Id { get; set; }

        public Guid ProjectId { get; set; }
        public string? StakeholderId { get; set; }

        public string Email { get; set; }
        public string DisplayName { get; set; }

        public string? RoleTitle { get; set; }

        public string? Company { get; set; }

        public ProjectReviewPermission Permission { get; set; }
        public InvitationStatus Status { get; set; } = InvitationStatus.Pending;

        public string ReviewToken { get; set; }
        public string ReviewUrl { get; set; }

        public DateTime? ExpiresAt { get; set; }
        public DateTime? AcceptedAt { get; set; }
        public DateTime? RevokedAt { get; set; }

        public string InvitedById { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public Project? Project  { get; private set; } = null!;
        public ApplicationUser InvitedBy { get; private set; } = null!;

        public void UpdateProjectReviewInvitation(string ReviewToken, string ReviewUrl , DateTime ExpiresAt)
        {
            this.ReviewToken = ReviewToken;
            this.ReviewUrl = ReviewUrl;
            this.ExpiresAt = ExpiresAt;
            this.UpdatedAt = DateTime.UtcNow;
        }
        public void Revoke()
        {
            this.Status = InvitationStatus.Revoked;
            this.RevokedAt = DateTime.UtcNow;
            this.UpdatedAt = DateTime.UtcNow;
            this.ReviewUrl = string.Empty;
        }

        public void Accept(string? displayName = null)
        {
            Status = InvitationStatus.Accepted;
            AcceptedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(displayName))
                DisplayName = displayName;
        }

    }
}
