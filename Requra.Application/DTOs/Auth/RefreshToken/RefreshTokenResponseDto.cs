using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.Auth.RefreshToken
{
    public class RefreshTokenResponseDto
    {
        public string UserId { get; set; }
        public string Name { get; set; }
        public bool IsAuthenticated { get; set; }
        public string Token { get; set; }
        public string RefreshToken { get; set; }
        public List<string> Roles { get; set; }
        public string? ProfilePicture { get; set; }
    }
}
