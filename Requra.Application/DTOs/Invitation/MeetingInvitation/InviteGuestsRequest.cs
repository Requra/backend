using Requra.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.Invitation.MeetingInvitation
{
    public class InviteGuestsRequest
    {
        public Guid MeetingId { get; set; }
        public string InvitedById { get; set; }
        public List<InviteGuestItemRequest> Guests { get; set; } = new();
        public ClientPlatform Platform { get; set; } = ClientPlatform.Web;
    }
    public class InviteGuestsApiRequest
    {
        public List<InviteGuestItemRequest> Guests { get; set; } = new();
        public ClientPlatform Platform { get; set; } = ClientPlatform.Web;
    }

    public class InviteGuestItemRequest
    {
        public string DisplayName { get; set; } = null!;
        public string Email { get; set; } = null!;
    }

}
