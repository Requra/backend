using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.UserStories
{
    public class UserStoryListItemDto
    {
        public Guid Id { get; set; }
        public string? SourceUserStoryId { get; set; }

        public Guid RequirementId { get; set; }
        public string? SourceRequirementId { get; set; }
        public string? RequirementTitle { get; set; }

        public string Title { get; set; } = string.Empty;
        public string? UserStory { get; set; }
        public string? Description { get; set; }

        public string Type { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public List<string> Labels { get; set; } = new();
        public int? StoryPoints { get; set; }

        public UserStoryJiraDto Jira { get; set; } = new();

        public List<UserStoryAcceptanceCriterionDto> AcceptanceCriteria { get; set; } = new();

        public string Status { get; set; } = string.Empty;
        public string? ReviewFeedback { get; set; }
        public string? ReviewedBy { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public int Version { get; set; }
        public DateTime UpdatedAt { get; set; }

        public UserStoryQualityDto Quality { get; set; } = new();
        public string? QualityStatus { get; set; }

        public List<UserStorySourceRefDto> SourceRefs { get; set; } = new();
    }
}
