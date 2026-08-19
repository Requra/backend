using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Requra.Application.DTOs.Auth.Login
{
    public class LogInResponseDTO
    {
        public string UserId { get; set; }=string.Empty;
        public string Name { get; set; }=string.Empty;
        public bool IsAuthenticated { get; set; }=false;
        public string Token { get; set; }=string.Empty; 
        public List<string> Roles { get; set; }=new List<string>();
        public string? ProfilePicture { get; set; }=string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime TokenExpiry { get; set; }

        public bool? IsNewUser {  get; set; }



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
