using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Requra.Application.DTOs.Auth.Login
{
    public class LogInResponseDTO
    {
        public string UserId { get; set; }
        public string Name { get; set; }
        public bool IsAuthenticated { get; set; }
        public string Token { get; set; }
        public List<string> Roles { get; set; }
        public string? ProfilePicture { get; set; }

        public LogInResponseDTO()
        {
            IsAuthenticated = false;
            Name = string.Empty;
            UserId = string.Empty;
            Roles = new List<string>();
            Token = string.Empty;
            ProfilePicture = string.Empty;
        }

    }
    
    public class LogInDTO
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}
