using AutoMapper;
using DocumentFormat.OpenXml.Office2010.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Requra.Application.DTOs.AI;
using Requra.Application.DTOs.Project.ProjectUpdate;
using Requra.Application.DTOs.ProjectReviewInvitaion;
using Requra.Application.Interfaces.IAnalysisRunService;
using Requra.Application.Response;
using Requra.Domain.Entities;
using Requra.Domain.Enums;
using Requra.Infrastructure.Data;
using System.Security.Claims;
using System.Text.Json;

namespace Requra.Presentation.Controllers.AIRuns
{
    [ApiController]
    [Route("api/projects/{projectId}/ai")]
    public class AiRunsController : ControllerBase
    {
        private readonly IAnalysisRunService _service;
        private readonly RequraDbContext _dbContext;
        public AiRunsController(IAnalysisRunService service)
        {
            _service = service;
        }

        [HttpPost("runs")]
        public async Task<IActionResult> StartRun(string projectId,StartRunRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(Response<AnalysisRunDto>.Failure(null, "Unauthorized User", 401));

            if (!Guid.TryParse(projectId, out var projectGuid))
            {
                return BadRequest(Response<AnalysisRunDto>.Failure(null,
                    "Invalid project id format",
                    400
                ));
            }
            //var userId = "027b0157-7553-4fbd-a171-6e3e8777911c"; //testing only
            var response = await _service.StartRunAsync(request, projectGuid, userId);

            return StatusCode(response.StatusCode, response);
        }

        [HttpGet("runs/{runId}")]
        public async Task<IActionResult> GetRun(string projectId, string runId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(Response<AnalysisRunDto>.Failure(null, "Unauthorized User", 401));
            if (!Guid.TryParse(projectId, out var projectGuid))
            {
                return BadRequest(Response<AnalysisRunDto>.Failure(null,
                    "Invalid project id format",
                    400
                ));
            }
            if (!Guid.TryParse(runId, out var runGuid))
            {
                return BadRequest(Response<AnalysisRunDto>.Failure(null,
                    "Invalid run id format",
                    400
                ));
            }
            //var userId = "027b0157-7553-4fbd-a171-6e3e8777911c"; //testing only

            return Ok(await _service.GetRunAsync(projectGuid, runGuid, userId));
        }

        [HttpGet("results-dashboard")]
        public async Task<IActionResult> GetResults(string projectId, [FromQuery] string? runId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(Response<JobResultResponseDto?>.Failure(null, "Unauthorized User", 401));
            if (!Guid.TryParse(projectId, out var projectGuid))
            {
                return BadRequest(Response<JobResultResponseDto>.Failure(null,
                    "Invalid project id format",
                    400
                ));
            }
            Guid? runGuid = null;

            if (!string.IsNullOrEmpty(runId))
               {  
                if (!Guid.TryParse(runId, out var parsedRunGuid))
                {
                    return BadRequest(Response<JobResultResponseDto>.Failure(null,
                        "Invalid run id format",
                        400
                    ));
                }
                runGuid = parsedRunGuid;
            }

            //var userId = "027b0157-7553-4fbd-a171-6e3e8777911c"; //testing only

            var response = await _service.GetResultAsync(projectGuid, runGuid, userId);

            return StatusCode(response.StatusCode, response);
        }

        [HttpPost("runs/{runId}/cancel")]
        public async Task<IActionResult> CancelRun(string projectId, string runId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(Response<CancelJobResponseDto?>.Failure(null, "Unauthorized User", 401));
            if (!Guid.TryParse(projectId, out var projectGuid))
            {
                return BadRequest(Response<CancelJobResponseDto>.Failure(null,
                    "Invalid project id format",
                    400
                ));
            }
            if (!Guid.TryParse(runId, out var runGuid))
            {
                return BadRequest(Response<CancelJobResponseDto>.Failure(null,
                    "Invalid run id format",
                    400
                ));
            }

            //var userId = "027b0157-7553-4fbd-a171-6e3e8777911c"; //testing only

            var response = await _service.CancelRunAsync(projectGuid, runGuid, userId);

            return StatusCode(response.StatusCode, response);
        }

        [HttpPost("runs/{runId}/retry")]
        public async Task<IActionResult> RetryRun(string projectId, string runId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(Response<RetryJobResponseDto>.Failure(null, "Unauthorized User", 401));
            if (!Guid.TryParse(projectId, out var projectGuid))
            {
                return BadRequest(Response<RetryJobResponseDto>.Failure(null,
                    "Invalid project id format",
                    400
                ));
            }
            if (!Guid.TryParse(runId, out var runGuid))
            {
                return BadRequest(Response<RetryJobResponseDto>.Failure(null,
                    "Invalid run id format",
                    400
                ));
            }

            //var userId = "027b0157-7553-4fbd-a171-6e3e8777911c"; //testing only

            var response = await _service.RetryRunAsync(projectGuid, runGuid, userId);

            return StatusCode(response.StatusCode, response);
        }

        //[HttpGet("results-dashboard/export")]
        //public async Task<IActionResult> ExportResultsDashboard(string projectId, [FromQuery] string? runId, [FromQuery] string? format)
        //{
        //    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //    if (string.IsNullOrEmpty(userId))
        //        return Unauthorized(Response<ExportResultsDto>.Failure(null, "Unauthorized User", 401));

        //    if (!Guid.TryParse(projectId, out var projectGuid))
        //    {
        //        return BadRequest(Response<ExportResultsDto>.Failure(null,
        //            "Invalid project id format",
        //            400
        //        ));
        //    }

        //    if (string.IsNullOrEmpty(runId))
        //    {
        //        return BadRequest(Response<ExportResultsDto>.Failure(null,
        //            "Run ID is required",
        //            400
        //        ));
        //    }

        //    if (!Guid.TryParse(runId, out var runGuid))
        //    {
        //        return BadRequest(Response<ExportResultsDto>.Failure(null,
        //            "Invalid run id format",
        //            400
        //        ));
        //    }

        //    if (string.IsNullOrEmpty(format))
        //    {
        //        return BadRequest(Response<ExportResultsDto>.Failure(null,
        //            "Format is required (xlsx or csv)",
        //            400
        //        ));
        //    }

        //    var response = await _service.ExportResultsDashboardAsync(projectGuid, runGuid, format.ToLower(), userId);

        //    return StatusCode(response.StatusCode, response);
        //}
    }
}
