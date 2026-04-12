using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.Project.ProjectResults.UserStory
{
    public class UserStoryDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public List<string> AcceptanceCriteria { get; set; } = new();
        public string Status { get; set; } = null!;
        public string Priority { get; set; } = null!;
        public string? Language { get; set; }
        public string CreatorName { get; set; } = null!;
        public string CreatorId { get;  set; } = null!;
      
        public Guid? RequirementId { get;  set; }
        public Guid? ProjectId { get;  set; }

        public string? JiraTicket { get;  set; }

        public DateTime? CreatedAt { get;  set; }

        public DateTime? UpdatedAt { get;  set; }
     


    }
}
