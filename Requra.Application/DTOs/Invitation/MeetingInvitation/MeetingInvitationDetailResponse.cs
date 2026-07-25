using System;

namespace Requra.Application.DTOs.Invitation.MeetingInvitation
{
   
    public class MeetingInvitationDetailResponse
    {
        public Guid Id { get; set; }
        public Guid MeetingId { get; set; }
        public string InviteeType { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? DisplayName { get; set; }
        public string? ProjectMemberId { get; set; }
        public string? StakeholderId { get; set; }
        public string Role { get; set; } = null!;
        public string Status { get; set; } = null!;
        public string InvitedById { get; set; } = null!;
        public DateTime? ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
