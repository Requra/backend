using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Requra.Application.DTOs.Project.ProjectResults.Feedbacks;
using Requra.Application.DTOs.ProjectReviewInvitaion;
using Requra.Application.Interfaces.IProjectService.IProjectReviewService;
using Requra.Domain.Enums;
using System.Security.Claims;

namespace Requra.Presentation.Controllers.Project.ProjectReview
{
    [ApiController]
    [Route("api/project-review")]
    [Authorize]
    public class ProjectReviewController : ControllerBase
    {
        private readonly IProjectReviewService _projectReviewService;

        public ProjectReviewController(IProjectReviewService projectReviewService)
        {
            _projectReviewService = projectReviewService;
        }

        [HttpPost("feedback")]
        public async Task<IActionResult> SubmitStakeholderFeedback([FromBody] SubmitStakeholderFeedbackApiRequest request,CancellationToken cancellationToken)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var requestWithUserId = new SubmitStakeholderFeedbackRequest
            {
                TargetId = request.TargetId,
                Content = request.Content,
                TargetType = request.TargetType,
                CurrentUserId = userId,

            };
            var response = await _projectReviewService.SubmitStakeholderFeedbackAsync(requestWithUserId, cancellationToken);

            return response.StatusCode switch
            {
                200 => Ok(response),
                201 => StatusCode(201, response),
                204 => NoContent(),
                400 => BadRequest(response),
                401 => Unauthorized(response),
                404 => NotFound(response),
                409 => Conflict(response),
                500 => StatusCode(500, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        [HttpGet("feedback")]
        public async Task<IActionResult> ListStakeholderFeedback([FromQuery] ListStakeholderFeedbackApiRequest request,CancellationToken cancellationToken)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var requestWithUserId = new ListStakeholderFeedbackRequest
            {
                Status = request.Status,
                ProjectId = request.ProjectId,
                AuthorId = userId,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };

            var response = await _projectReviewService.ListStakeholderFeedbackAsync(requestWithUserId, cancellationToken);

            return response.StatusCode switch
            {
                200 => Ok(response),
                201 => StatusCode(201, response),
                204 => NoContent(),
                400 => BadRequest(response),
                401 => Unauthorized(response),
                404 => NotFound(response),
                409 => Conflict(response),
                500 => StatusCode(500, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }
        [HttpPatch("{projectId:guid}/feedback/{feedbackId:guid}")]
        public async Task<IActionResult> UpdateStakeholderFeedbackStatus([FromRoute] Guid projectId,[FromRoute] Guid feedbackId,[FromBody] UpdateStakeholderFeedbackStatusAPIRequest request,CancellationToken cancellationToken)
        {
            var requestWithIds = new UpdateStakeholderFeedbackStatusRequest
            {
                ProjectId = projectId,
                FeedbackId = feedbackId,
                UserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                Status = request.Status,
                ResolutionNote = request.ResolutionNote,
                IsRead = request.IsRead,
            };

            var response = await _projectReviewService.UpdateStakeholderFeedbackStatusAsync(requestWithIds, cancellationToken);

            return response.StatusCode switch
            {
                200 => Ok(response),
                201 => StatusCode(201, response),
                204 => NoContent(),
                400 => BadRequest(response),
                401 => Unauthorized(response),
                404 => NotFound(response),
                409 => Conflict(response),
                500 => StatusCode(500, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        [HttpGet("{projectId:guid}/feedback")]
        public async Task<IActionResult> ListProjectStakeholderFeedback([FromRoute] Guid projectId,[FromQuery] StakeholderFeedbackStatus? status,[FromQuery] FeedbackTargetType? targetType,[FromQuery] bool? isRead,[FromQuery] int pageNumber = 1,[FromQuery] int pageSize = 20,CancellationToken cancellationToken = default)
        {
            var request = new ListProjectStakeholderFeedbackRequest
            {
                ProjectId = projectId,
                UserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                Status = status,
                TargetType = targetType,
                IsRead = isRead,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var response = await _projectReviewService.ListProjectStakeholderFeedbackAsync(request, cancellationToken);

            return response.StatusCode switch
            {
                200 => Ok(response),
                201 => StatusCode(201, response),
                204 => NoContent(),
                400 => BadRequest(response),
                401 => Unauthorized(response),
                404 => NotFound(response),
                409 => Conflict(response),
                500 => StatusCode(500, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }
        [HttpGet("{token}")]
        [AllowAnonymous]
        public async Task<IActionResult> PreviewProjectReviewInvitation([FromRoute] string token,CancellationToken cancellationToken)
        {
            var request = new PreviewProjectReviewInvitationRequest
            {
                Token = token
            };

            var response = await _projectReviewService.PreviewProjectReviewInvitationAsync(request, cancellationToken);

            return response.StatusCode switch
            {
                200 => Ok(response),
                201 => StatusCode(201, response),
                204 => NoContent(),
                400 => BadRequest(response),
                401 => Unauthorized(response),
                404 => NotFound(response),
                409 => Conflict(response),
                500 => StatusCode(500, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        [HttpPost("{token}/accept")]
        [AllowAnonymous]
        public async Task<IActionResult> AcceptProjectReviewInvitation([FromRoute] string token,[FromBody] AcceptProjectReviewInvitationAPIRequest request,CancellationToken cancellationToken)
        {
            var acceptRequest = new AcceptProjectReviewInvitationRequest
            {
                Token = token,
                DisplayName = request.DisplayName
            };

            var response = await _projectReviewService.AcceptProjectReviewInvitationAsync(acceptRequest, cancellationToken);

            return response.StatusCode switch
            {
                200 => Ok(response),
                201 => StatusCode(201, response),
                204 => NoContent(),
                400 => BadRequest(response),
                401 => Unauthorized(response),
                404 => NotFound(response),
                409 => Conflict(response),
                500 => StatusCode(500, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        [HttpGet("{token}/results-dashboard")]
        [AllowAnonymous]
        public async Task<IActionResult> GetProjectReviewDashboard([FromRoute] string token,CancellationToken cancellationToken)
        {
            var request = new GetProjectReviewDashboardRequest
            {
                Token = token
            };

            var response = await _projectReviewService.GetProjectReviewDashboardAsync(request, cancellationToken);

            return response.StatusCode switch
            {
                200 => Ok(response),
                201 => StatusCode(201, response),
                204 => NoContent(),
                400 => BadRequest(response),
                401 => Unauthorized(response),
                404 => NotFound(response),
                409 => Conflict(response),
                500 => StatusCode(500, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }
    }
}
