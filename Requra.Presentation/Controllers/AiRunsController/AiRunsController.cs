using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Requra.Application.DTOs.AI;
using Requra.Application.Interfaces.IAnalysisRunService;
using Requra.Domain.Enums;
using Requra.Infrastructure.Data;
using System.Text.Json;

namespace Requra.Presentation.Controllers.AiRunsController
{
    [ApiController]
    [Route("api/projects/{projectId}/ai")]
    public class AiRunsController : ControllerBase //Temp context for testing only
    {
        private readonly IAnalysisRunService _service;
        private readonly RequraDbContext _dbContext;
        public AiRunsController(IAnalysisRunService service, RequraDbContext dbContext)
        {
            _service = service;
            _dbContext = dbContext;

        }

        [HttpPost("runs")]
        public async Task<IActionResult> StartRun(Guid projectId, [FromBody] StartRunRequest request)
        {
            var response = await _service.StartRunAsync(projectId,request);

            return StatusCode(response.StatusCode, response);
        }

        [HttpGet("runs/{runId}")]
        public async Task<IActionResult> GetRun(Guid projectId, Guid runId)
        {
            return Ok(await _service.GetRunAsync(projectId, runId));
        }

        [HttpGet("results-dashboard")]
        public async Task<IActionResult> GetResults(Guid projectId, [FromQuery] Guid? runId)
        {
            var response = await _service.GetResultAsync(projectId, runId);

            return StatusCode(response.StatusCode, response);
        }
    }
}
