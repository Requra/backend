using Requra.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.Invitation.MeetingInvitation
{
    public class InviteMeetingParticipantsResponse
    {
        public List<MeetingInvitationItemResponse> Items { get; set; } = new();
    }
    public class MeetingInvitationItemResponse
    {
        public Guid Id { get; set; }
        public Guid MeetingId { get; set; }
        public InviteType InviteType { get; set; }
        public string Email { get; set; } = null!;
        public string? DisplayName { get; set; }
        public string? ProjectMemberId { get; set; }
        public string? StakeholderId { get; set; }
        public MeetingRole Role { get; set; }
        public InvitationStatus Status { get; set; }
        public string InvitedById { get; set; } = null!;
        public DateTime? ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
