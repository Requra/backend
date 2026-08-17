using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.UserStories
{
    public class UserStorySourceRefDto
    {
        public string? SourceId { get; set; }
        public string? SourceType { get; set; }
        public string? DocumentName { get; set; }
        public int? Page { get; set; }
        public string? ChunkId { get; set; }
        public string? Quote { get; set; }
        public double? ConfidenceScore { get; set; }
    }
    public class UserStoryQualityDto
    {
        public double? Score { get; set; }
        public List<string> Issues { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }
    public class UserStoryJiraDto
    {
        public string? IssueType { get; set; }
        public int? StoryPoints { get; set; }
        public List<string> Labels { get; set; } = new();
    }
}
