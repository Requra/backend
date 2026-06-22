using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Requra.Application.DTOs.AI;
using Requra.Application.Interfaces.IAnalysisRunService;
using System.Text.Json;

namespace Requra.Presentation.Controllers.AiRunsController
{
    [ApiController]
    [Route("api/projects/{projectId}/ai")]
    public class AiRunsController : ControllerBase
    {
        private readonly IAnalysisRunService _service;

        public AiRunsController(IAnalysisRunService service)
        {
            _service = service;
        }

        [HttpPost("runs")]
        public async Task<IActionResult> StartRun(Guid projectId, [FromBody] StartRunRequest request)
        {
            var response = await _service.StartRunAsync(
                projectId,
                request.DocumentIds,
                request.MeetingId);

            return StatusCode(response.StatusCode, response);
        }

        [HttpGet("runs/{runId}")]
        public async Task<IActionResult> GetRun(Guid runId)
        {
            return Ok(await _service.GetRunAsync(runId));
        }

        [HttpGet("results-dashboard")]
        public async Task<IActionResult> GetResults(Guid runId)
        {
            var result = await _service.GetResultAsync(runId);

            var data = JsonSerializer.Deserialize<ProcessJsonResponse>(result.RawJson);

            return Ok(data);
        }
    }
}
