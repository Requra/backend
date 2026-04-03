using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Requra.Application.DTOs;
using Requra.Application.DTOs.Project;
using Requra.Application.Interfaces.IProjectService;
using Requra.Application.Response;
using System;
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
    }
}