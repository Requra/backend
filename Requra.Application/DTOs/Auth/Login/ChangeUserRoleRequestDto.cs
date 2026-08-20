using Requra.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.Auth.Login
{
    public class ChangeUserRoleRequestDto
    {
        public string UserId { get; set; } = string.Empty;
        public UserRole Role { get; set; }
    }
    public class ChangeUserRoleRequestApiDto
    {
        public UserRole Role { get; set; }
    }
}
