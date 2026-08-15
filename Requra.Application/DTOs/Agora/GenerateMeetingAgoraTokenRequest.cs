using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.Agora
{
    public class GenerateMeetingAgoraTokenRequest
    {
        public Guid MeetingId { get; set; }
        public string CurrentUserId { get; set; } = string.Empty;
        public string? GuestToken { get; set; }
    }
}
