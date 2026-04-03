using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.Project
{
    public class ProjectDTO
    {
        public Guid? Id { get; set; }
        public string? Name { get; set; } = null!;
        public string? Description { get; set; }
        public string Status { get; set; } = null!;
        public string? ClientName { get; set; }
        public int? TotalRequirements { get; set; }
        public int? TotalUserStories { get; set; }
        public int? TotalComments { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
