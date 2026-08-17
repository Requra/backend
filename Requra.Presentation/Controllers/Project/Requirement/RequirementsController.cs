using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Requra.Application.DTOs.Project.Requirements;
using Requra.Application.Interfaces.IProjectService.IRequirementService;

namespace Requra.Presentation.Controllers.Project.Requirement
{
    [Route("api/[controller]")]
    [ApiController]
    public class RequirementsController : ControllerBase
    {
        private readonly IRequirementService _requirementService;

        public RequirementsController(IRequirementService requirementService)
        {
            _requirementService = requirementService;
        }

        [HttpPatch("{projectId:guid}/requirements/{requirementId:guid}/status")]
        [Authorize]
        public async Task<IActionResult> UpdateRequirementStatus([FromRoute] Guid projectId, [FromRoute] Guid requirementId, [FromHeader(Name = "If-Match")] string? ifMatch, [FromBody] UpdateRequirementStatusApiRequest request, CancellationToken cancellationToken)
        {
            var requestResponse = new UpdateRequirementStatusRequest
            {
                IfMatch = ifMatch,
                ProjectId = projectId,
                RequirementId = requirementId,
                ReviewedById= User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                WorkflowStatus = request.WorkflowStatus,
                ReviewFeedback = request.ReviewFeedback
            };

            var response = await _requirementService.UpdateRequirementStatusAsync(requestResponse, cancellationToken);

            if (response.IsSuccess && response.Data != null)
            {
                Response.Headers.ETag = $"\"{response.Data.Version}\"";
            }

            return response.StatusCode switch
            {
                200 => Ok(response),
                201 => StatusCode(201, response),
                204 => NoContent(),
                400 => BadRequest(response),
                401 => Unauthorized(response),
                403 => StatusCode(StatusCodes.Status403Forbidden, response),
                404 => NotFound(response),
                409 => Conflict(response),
                412 => StatusCode(StatusCodes.Status412PreconditionFailed, response),
                428 => StatusCode(StatusCodes.Status428PreconditionRequired, response),
                500 => StatusCode(500, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        [HttpPatch("{projectId:guid}/requirements/{requirementId:guid}")]
        [Authorize]
        public async Task<IActionResult> EditRequirementContent([FromRoute] Guid projectId, [FromRoute] Guid requirementId, [FromHeader(Name = "If-Match")] string? ifMatch, [FromBody] EditRequirementContentApiRequest request, CancellationToken cancellationToken)
        {
            var CurrentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var requestResponse =new EditRequirementContentRequest
            {
                ProjectId = projectId,
                RequirementId = requirementId,
                IfMatch = ifMatch,
                CurrentUserId =CurrentUserId,
                Title = request.Title,
                Description = request.Description,
                Type = request.Type,
                Priority = request.Priority
            };
            {

            }

            var response = await _requirementService.EditRequirementContentAsync(requestResponse, cancellationToken);

            if (response.IsSuccess && response.Data != null)
            {
                Response.Headers.ETag = $"\"{response.Data.Version}\"";
            }

            return response.StatusCode switch
            {
                200 => Ok(response),
                201 => StatusCode(201, response),
                204 => NoContent(),
                400 => BadRequest(response),
                401 => Unauthorized(response),
                403 => StatusCode(StatusCodes.Status403Forbidden, response),
                404 => NotFound(response),
                409 => Conflict(response),
                412 => StatusCode(StatusCodes.Status412PreconditionFailed, response),
                428 => StatusCode(StatusCodes.Status428PreconditionRequired, response),
                500 => StatusCode(StatusCodes.Status500InternalServerError, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        [HttpGet("projects/{projectId:guid}/requirements")]
        [Authorize]
        public async Task<IActionResult> GetRequirementsByProjectId([FromRoute] Guid projectId,[FromQuery] int pageNumber = 1,[FromQuery] int pageSize = 25,[FromQuery] List<string>? status = null,[FromQuery] string? search = null)
        {
            var request = new GetProjectRequirementsRequest
            {
                ProjectId = projectId,
                PageNumber = pageNumber,
                PageSize = pageSize,
                Status = status,
                Search = search
            };

            var response = await _requirementService.GetRequirementsByProjectIdAsync(request);
          
            return response.StatusCode switch
            {
                200 => Ok(response),
                201 => StatusCode(201, response),
                204 => NoContent(),
                400 => BadRequest(response),
                401 => Unauthorized(response),
                403 => StatusCode(StatusCodes.Status403Forbidden, response),
                404 => NotFound(response),
                409 => Conflict(response),
                412 => StatusCode(StatusCodes.Status412PreconditionFailed, response),
                428 => StatusCode(StatusCodes.Status428PreconditionRequired, response),
                500 => StatusCode(StatusCodes.Status500InternalServerError, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }
    }
}
