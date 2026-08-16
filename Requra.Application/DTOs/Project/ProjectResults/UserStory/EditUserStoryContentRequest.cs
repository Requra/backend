using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.DTOs.Project.ProjectResults.UserStory
{
    public class AcceptanceCriterionRequestDto
    {
        public string? Id { get; set; }
        public string? Text { get; set; }
        public string? Format { get; set; }
    }

    /// <summary>
    /// Exact shape of the PATCH /user-stories/{storyId} JSON body.
    /// Every field is optional; only fields present are applied (partial update).
    /// </summary>
    public class EditUserStoryContentBody
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public List<AcceptanceCriterionRequestDto>? AcceptanceCriteria { get; set; }
        public string? Priority { get; set; }
        public List<string>? Labels { get; set; }
    }

    /// <summary>
    /// Internal command combining the request body with route/header/identity context.
    /// Not bound directly from JSON - built by the controller.
    /// </summary>
    public class EditUserStoryContentRequest
    {
        public Guid ProjectId { get; set; }
        public Guid StoryId { get; set; }
        public string? IfMatch { get; set; }
        public string? ModifiedById { get; set; }

        public string? Title { get; set; }
        public string? Description { get; set; }
        public List<AcceptanceCriterionRequestDto>? AcceptanceCriteria { get; set; }
        public string? Priority { get; set; }
        public List<string>? Labels { get; set; }

        public static EditUserStoryContentRequest FromBody(
            Guid projectId, Guid storyId, string? ifMatch, string? modifiedById, EditUserStoryContentBody body) =>
            new()
            {
                ProjectId = projectId,
                StoryId = storyId,
                IfMatch = ifMatch,
                ModifiedById = modifiedById,
                Title = body.Title,
                Description = body.Description,
                AcceptanceCriteria = body.AcceptanceCriteria,
                Priority = body.Priority,
                Labels = body.Labels
            };
    }
}
