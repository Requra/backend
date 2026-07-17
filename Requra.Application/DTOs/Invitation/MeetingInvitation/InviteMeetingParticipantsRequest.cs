using Requra.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.Invitation.MeetingInvitation
{
    public class InviteMeetingParticipantsRequest
    {
        public Guid MeetingId { get; set; }
        public string InvitedById { get; set; }
        public List<InviteMeetingParticipantItemRequest> Members { get; set; } = new();
    }
    public class InviteMeetingParticipantsApiRequest
    {
        public List<InviteMeetingParticipantItemRequest> Members { get; set; } = new();
    }

    public class InviteMeetingParticipantItemRequest
    {
        public string MemberId { get; set; } = null!;
        public ProjectRole Role { get; set; }
        
    }
}
