using Requra.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.Profile
{
    public class ProfileDto
    {
        public string Id { get; set; }=string.Empty;
        public string Name { get; set; }= string.Empty;
        public string Email { get; set; }=string.Empty;
        public UserRole JobTitle { get; set; }= UserRole.None;
        public string AvatarUrl { get; set; }= string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
