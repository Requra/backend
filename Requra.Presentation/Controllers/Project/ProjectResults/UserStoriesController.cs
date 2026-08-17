using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Requra.Application.DTOs.Project.ProjectResults.UserStory;
using Requra.Application.DTOs.UserStories;
using Requra.Application.Interfaces.IProjectService.IProjectResultsService.IUserStoryService;
using Requra.Application.Response;

namespace Requra.Presentation.Controllers.Project.ProjectResults
{
    [Route("api/projects")]
    [ApiController]
    [Authorize]
    public class UserStoriesController : ControllerBase
    {
        private readonly IUserStoryService _userStoryService;

        public UserStoriesController(IUserStoryService userStoryService)
        {
            _userStoryService = userStoryService;
        }

        [HttpGet("{projectId:guid}/results/user-stories")]
        public async Task<IActionResult> GetUserStoriesByProjectId([FromRoute] Guid projectId,[FromQuery] int pageNumber = 1,[FromQuery] int pageSize = 25,[FromQuery] List<string>? status = null,[FromQuery] string? search = null)
        {
            var request = new GetProjectUserStoriesRequest
            {
                ProjectId = projectId,
                PageNumber = pageNumber,
                PageSize = pageSize,
                Status = status,
                Search = search
            };

            var response = await _userStoryService.GetUserStoriesByProjectIdAsync(request);

            return response.StatusCode switch
            {
                200 => Ok(response),
                204 => NoContent(),
                400 => BadRequest(response),
                404 => NotFound(response),
                500 => StatusCode(500, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        [HttpPatch("~/api/projects/{projectId:guid}/user-stories/{storyId:guid}/status")]
        public async Task<IActionResult> UpdateUserStoryStatus(
            [FromRoute] Guid projectId,
            [FromRoute] Guid storyId,
            [FromHeader(Name = "If-Match")] string? ifMatch,
            [FromBody] UpdateUserStoryStatusRequest request,
            CancellationToken cancellationToken)
        {
            request.ProjectId = projectId;
            request.StoryId = storyId;
            request.IfMatch = ifMatch;
            request.ReviewedById = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            var response = await _userStoryService.UpdateUserStoryStatusAsync(request, cancellationToken);

            if (response.IsSuccess && response.Data != null)
            {
                Response.Headers.ETag = $"\"{response.Data.Version}\"";
            }

            return response.StatusCode switch
            {
                200 => Ok(response),
                400 => BadRequest(response),
                401 => Unauthorized(response),
                403 => StatusCode(StatusCodes.Status403Forbidden, response),
                404 => NotFound(response),
                409 => Conflict(response),
                422 => UnprocessableEntity(response),
                500 => StatusCode(StatusCodes.Status500InternalServerError, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }
        [HttpPatch("~/api/projects/{projectId:guid}/user-stories/{storyId:guid}")]
        public async Task<IActionResult> EditUserStoryContent(
           [FromRoute] Guid projectId,
           [FromRoute] Guid storyId,
           [FromHeader(Name = "If-Match")] string? ifMatch,
           [FromBody] EditUserStoryContentBody body,
           CancellationToken cancellationToken)
        {
            var modifiedById = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var request = EditUserStoryContentRequest.FromBody(projectId, storyId, ifMatch, modifiedById, body);

            var response = await _userStoryService.EditUserStoryContentAsync(request, cancellationToken);

            if (response.IsSuccess && response.Data != null)
            {
                Response.Headers.ETag = $"\"{response.Data.Version}\"";
            }

            return response.StatusCode switch
            {
                200 => Ok(response),
                400 => BadRequest(response),
                401 => Unauthorized(response),
                403 => StatusCode(StatusCodes.Status403Forbidden, response),
                404 => NotFound(response),
                409 => Conflict(response),
                422 => UnprocessableEntity(response),
                500 => StatusCode(StatusCodes.Status500InternalServerError, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }
    }
}
