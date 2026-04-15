using Requra.Application.DTOs.Project.ProjectCreation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.Project.ProjectDetails
{
    public class ProjectDetailsDto
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string? Description { get; set; } = "";

        public string ProjectType { get; set; } = "";
        public string Status { get; set; } = "";
        public string ClientName { get; set; } = "";

        public List<TeamMemberDto> TeamMembers { get; set; } = new();

        public DateTime CreatedAt { get; set; }
    }
}
