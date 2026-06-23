using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.AI
{
    public class UserStoryDto
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public List<string> AcceptanceCriteria { get; set; }
        public string Priority { get; set; }
        public string RequirementId { get; set; }
    }
}
