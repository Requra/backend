using Requra.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.Project.ProjectCreation
{
    public class ProjectRequestDto
    {
        public string Name { get; set; } = null!;
        public string? Description { get; set; }

        public string ClientEmail { get; set; } = null!;

        public ProjectType ProjectType { get; set; } = ProjectType.None;

        public List<TeamMemberDto> TeamMembers { get; set; } = new  ();
    }
}
