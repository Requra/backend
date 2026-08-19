using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.LiveKit
{
    public class LiveKitTokenResponseDto
    {
        public string ServerUrl { get; set; } = null!;
        public string Token { get; set; } = null!;
        public string RoomName { get; set; } = null!;
        public string Identity { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
        public DateTime MeetingEndsAt { get; set; }
        public string DisplayName { get; set; } = null!;
    }
}
