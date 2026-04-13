using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.Auth.RefreshToken
{
    public class RefreshTokenRequestDto
    {
        public string   AccessToken { get; set; }

        public string RefreshToken { get; set; }
      
    }
}
