using Requra.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Requra.Application.DTOs.Auth.Login
{
    public class LoginRequestDto
    {
        [Required]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
        public ClientPlatform Platform { get; set; } = ClientPlatform.Web;
    }
}
