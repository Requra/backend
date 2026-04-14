using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.Auth.RefreshToken
{
    public class RefreshTokenResponseDto
    {
        public string UserId { get; set; }= string.Empty;
        public string Name { get; set; }= string.Empty;
        public bool IsAuthenticated { get; set; }
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new List<string>();
        public string? ProfilePicture { get; set; }= string.Empty;
    }
}
