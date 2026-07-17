using Requra.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Domain.Entities
{
    public class Invitation
    {
        public Guid Id { get; private set; }
        public Guid? MeetingId { get; private set; }

        public InviteType? InviteType { get; private set; }

        public string Email { get; private set; } = null!;
        public string? DisplayName { get; private set; }

        public string? ProjectMemberId { get; private set; }
        public string? StakeholderId { get; private set; }

        public MeetingRole? Role { get; private set; }
        public InvitationStatus Status { get; private set; }

        public string InvitedById { get; private set; } = null!;

        public DateTime? ExpiresAt { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }

        // Navigation
        public MeetingSession? Meeting { get; private set; } = null!;
        public ApplicationUser InvitedBy { get; private set; } = null!;

        private Invitation()
        {
        }

        public Invitation(
            Guid meetingId,
            InviteType? inviteType,
            string email,
            string? displayName,
            string? projectMemberId,
            string? stakeholderId,
            MeetingRole? role,
            string? invitedById,
            DateTime? expiresAt = null)
        {
            Id = Guid.NewGuid();
            MeetingId = meetingId;
            InviteType = inviteType;
            Email = email;
            DisplayName = displayName;
            ProjectMemberId = projectMemberId;
            StakeholderId = stakeholderId;
            Role = role;
            InvitedById = invitedById;
            ExpiresAt = expiresAt;
            Status = InvitationStatus.Pending;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public void MarkAccepted()
        {
            Status = InvitationStatus.Accepted;
            UpdatedAt = DateTime.UtcNow;
        }

        public void MarkDeclined()
        {
            Status = InvitationStatus.Declined;
            UpdatedAt = DateTime.UtcNow;
        }

        public void MarkExpired()
        {
            Status = InvitationStatus.Expired;
            UpdatedAt = DateTime.UtcNow;
        }

        public void MarkRevoked()
        {
            Status = InvitationStatus.Revoked;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
