using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Domain.Entities
{
    public class JiraFields
    {
        public Guid Id { get; private set; }

        public List<string> Labels { get; private set; } = new();

        public string? Summary { get; private set; }

        public string? Priority { get; private set; }

        public string? EpicName { get; private set; }

        public List<string> Components { get; private set; } = new();

        public string? IssueType { get; private set; }

        public string? Description { get; private set; }

        public int? StoryPoints { get; private set; }

        public Guid UserStoryId { get; private set; }

        public UserStory UserStory { get; private set; } = null!;
    }
}
