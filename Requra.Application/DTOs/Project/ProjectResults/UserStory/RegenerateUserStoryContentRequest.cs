using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.Project.ProjectResults.UserStory
{
    public class RegenerateUserStoryContentBody
    {
        public string Feedback { get; set; } = null!;
    }
    public class RegenerateUserStoryContentRequest
    {
        public Guid ProjectId { get; set; }
        public Guid StoryId { get; set; }
        public string? IfMatch { get; set; }
        public string? IdempotencyKey { get; set; }
        public string? ModifiedById { get; set; }
        public string Feedback { get; set; } = null!;

        public static RegenerateUserStoryContentRequest FromBody(
            Guid projectId, Guid storyId, string? ifMatch, string? idempotencyKey, string? modifiedById, RegenerateUserStoryContentBody body) =>
            new()
            {
                ProjectId = projectId,
                StoryId = storyId,
                IfMatch = ifMatch,
                IdempotencyKey = idempotencyKey,
                ModifiedById = modifiedById,
                Feedback = body.Feedback
            };
    }
}
