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
        public async Task<IActionResult> UpdateRequirementStatus([FromRoute] Guid projectId, [FromRoute] Guid requirementId, [FromHeader(Name = "If-Match")] string? ifMatch, [FromBody] UpdateRequirementStatusRequest request, CancellationToken cancellationToken)
        {
            request.ProjectId = projectId;
            request.RequirementId = requirementId;
            request.IfMatch = ifMatch;
            request.ReviewedById = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            var response = await _requirementService.UpdateRequirementStatusAsync(request, cancellationToken);

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
        public async Task<IActionResult> EditRequirementContent(
        [FromRoute] Guid projectId,
        [FromRoute] Guid requirementId,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        [FromBody] EditRequirementContentRequest request,
        CancellationToken cancellationToken)
        {
            request.ProjectId = projectId;
            request.RequirementId = requirementId;
            request.IfMatch = ifMatch;
            request.CurrentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            var response = await _requirementService.EditRequirementContentAsync(request, cancellationToken);

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
    }
}
