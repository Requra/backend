using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.ProjectMembers
{
    public class ProjectMemberDto
    {
        public string UserId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string ProjectRole { get; set; } = string.Empty;

        public string avatarUrl { get; set; } = string.Empty;
    }
}
