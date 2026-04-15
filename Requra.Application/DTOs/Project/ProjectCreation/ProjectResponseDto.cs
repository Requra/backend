using Requra.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.Project.ProjectCreation
{
    public class ProjectResponseDto
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string? Description { get; set; } = "";
        public ProjectType ProjectType { get; set; } = ProjectType.None;

        public string Status { get; set; } = "";
        public string ClientEmail { get; set; } = "";
        public DateTime CreatedAt { get; set; } 
    }
}
