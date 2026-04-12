using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Requra.Application.Interfaces.IProjectService.IProjectResultsService.IUserStoryService;
using Requra.Application.Response;

namespace Requra.Presentation.Controllers.Project.ProjectResults
{
    [Route("api/projects/{projectId}/results/user-stories")]
    [ApiController]
    public class UserStoriesController : ControllerBase
    {
        private readonly IUserStoryService _userStoryService;

        public UserStoriesController(IUserStoryService userStoryService)
        {
            _userStoryService = userStoryService;
        }

        [HttpGet]
        public async Task<IActionResult> GetUserStoriesByProjectId(Guid projectId)
        {
            var response = await _userStoryService.GetUserStoriesByProjectIdAsync(projectId);

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
    }
}
