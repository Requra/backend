using System;

namespace Requra.Application.DTOs.Invitation.MeetingInvitation
{
    public class AcceptMeetingInvitationResponse
    {
        public Guid InvitationId { get; set; }
        public Guid MeetingId { get; set; }
        public string Status { get; set; } = null!;
        public string? ParticipantId { get; set; }
    }
}
