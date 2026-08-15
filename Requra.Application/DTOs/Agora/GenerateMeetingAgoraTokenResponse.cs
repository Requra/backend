using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.Agora
{
    public class AgoraRtcTokenResponseDto
    {
        public string AppId { get; set; } = string.Empty;
        public string ChannelName { get; set; } = string.Empty;
        public string Uid { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }
}
