using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.Invitation.MeetingInvitation
{
    public class MeetingInvitationPreviewResponse
    {
        public Guid MeetingId { get; set; }
        public string? MeetingTitle { get; set; }
        public string? ProjectName { get; set; }
        public DateTime? ScheduledAt { get; set; }
        public string InviteeEmail { get; set; } = null!;
        public string? InviteeDisplayName { get; set; }
        public string? InviteeType { get; set; }
        public string? Role { get; set; }
        public string Status { get; set; } = null!;
        public DateTime? ExpiresAt { get; set; }
    }
}
