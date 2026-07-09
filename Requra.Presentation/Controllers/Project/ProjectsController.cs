using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Requra.Application.DTOs;
using Requra.Application.DTOs.Project;
using Requra.Application.DTOs.Project.ProjectCreation;
using Requra.Application.DTOs.Project.ProjectDetails;
using Requra.Application.DTOs.Project.ProjectUpdate;
using Requra.Application.DTOs.ProjectMembers;
using Requra.Application.Interfaces.IProjectService;
using Requra.Application.Response;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Requra.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProjectsController : ControllerBase
    {
        private readonly IProjectService _projectService;
        private readonly ILogger<ProjectsController> _logger;

        public ProjectsController(IProjectService projectService, ILogger<ProjectsController> logger)
        {
            _projectService = projectService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetProjects([FromQuery] ProjectFilter filter)
        {
            if (filter == null)
            {
                return BadRequest(Response<PagedResult<ProjectDTO>>.Failure("Filter is required", 400));
            }

            var result = await _projectService.GetUserProjectsAsync(filter);

            if (result.IsSuccess)
            {
                return Ok(result);
            }
            else
            {
                _logger.LogWarning("Failed to get projects: {Message}", result.Message);
                return StatusCode(result.StatusCode, result);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ProjectRequestDto request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return StatusCode(401, Response<string>.Failure("","User not authenticated", 401));
            }

            var result = await _projectService.CreateProjectAsync(request, userId);

            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            if (!Guid.TryParse(id, out var guid))
            {
                return BadRequest(Response<ProjectDetailsDto>.Failure(new ProjectDetailsDto(),
                    "Invalid project id format",
                    400
                ));
            }
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(Response<bool>.Failure(false, "Unauthorized User", 401));
            }
            var result = await _projectService.GetProjectByIdAsync(guid, userId);

            return StatusCode(result.StatusCode, result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProject(string id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(Response<bool>.Failure(false, "Unauthorized User", 401));
            }
            if (!Guid.TryParse(id, out var guid))
            {
                return BadRequest(Response<bool>.Failure(false, "Invalid project id format", 400));
            }

            var result = await _projectService.DeleteProjectAsync(guid, userId);

            return StatusCode(result.StatusCode, result);
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateProject(string id, [FromBody] ProjectUpdateRequestDto dto)
        {
            if (!Guid.TryParse(id, out var guid))
            {
                return BadRequest(Response<ProjectUpdateResponseDto>.Failure(new(),
                    "Invalid project id format",
                    400
                ));
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(Response<ProjectUpdateResponseDto>.Failure(new(), "Unauthorized User", 401));
            }
            var result = await _projectService.UpdateProjectAsync(guid, dto, userId);

            return StatusCode(result.StatusCode, result);
        }
        [HttpGet("{projectId}/members")]
        public async Task<IActionResult> GetProjectMembers(Guid projectId,[FromQuery] GetProjectMembersQuery query)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(Response<PagedResult<ProjectMemberDto>>.Failure(null, "Unauthorized User", 401));
            }
            //var userId = "01f535f1-9870-4141-9b29-21df2d9cd6ec"; // Hardcoded userId for testing purposes

            if (projectId == Guid.Empty || !Guid.TryParse(projectId.ToString(), out Guid validGuid))
                return StatusCode(422, Response<PagedResult<ProjectMemberDto>>.Failure(null, "Validation failed", 422, new List<string> { "Invalid projectId" }));

            var response = await _projectService.GetProjectMembersAsync(projectId, query, userId);

            return StatusCode(response.StatusCode, response);
        }
    }
}