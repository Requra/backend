using Requra.Application.DTOs.Project.ProjectCreation;
using Requra.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.Project.ProjectUpdate
{
    public class ProjectUpdateResponseDto
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string? Description { get; set; } = "";
        public ProjectType ProjectType { get; set; } = ProjectType.None;
        public ProjectStatus Status { get; set; } = ProjectStatus.None;
        public string? ClientEmail { get; set; } = "";

        public List<TeamMemberDto> TeamMembers { get; set; } = new();

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt
        {
            get; set;

        }
    }
}
