using Microsoft.EntityFrameworkCore.Query;
using Requra.Application.DTOs.Project.ProjectCreation;
using Requra.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.Project.ProjectUpdate
{
    public class ProjectUpdateRequestDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public ProjectType? ProjectType { get; set; }
        public ProjectStatus? Status { get; set; }

        public string ? ClientEmail { get; set; } 
        public Language? Language { get; set; }

        public List<TeamMemberDto>? TeamMembers { get; set; }

    }
}
