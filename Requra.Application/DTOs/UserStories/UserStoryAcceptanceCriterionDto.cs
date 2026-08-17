using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.UserStories
{
    public class UserStoryAcceptanceCriterionDto
    {
        public string Id { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string? Format { get; set; }
    }
}
