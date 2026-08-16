using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Domain.Entities
{
    public class UserStoryQuality
    {
        public Guid Id { get; private set; }

        public double Score { get; private set; }

        public List<string> Issues { get; private set; } = new();

        public List<string> Warnings { get; private set; } = new();

        public Guid UserStoryId { get; private set; }

        public UserStory UserStory { get; private set; } = null!;
    }
}
