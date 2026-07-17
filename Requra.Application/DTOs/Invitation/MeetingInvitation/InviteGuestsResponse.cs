using Requra.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.Invitation.MeetingInvitation
{
    public class InviteGuestsResponse
    {
        public List<MeetingInvitationItemResponse> Items { get; set; } = new();
    }

   
}
