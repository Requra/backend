using Microsoft.AspNetCore.Mvc;
using Requra.Application.DTOs.Invitation.MeetingInvitation;
using Requra.Application.Interfaces.IMeetingService;
using Requra.Application.Response;
using System.Security.Claims;

namespace Requra.Presentation.Controllers.Meeting
{
    // Routed separately from MeetingsController since these endpoints are addressed by
    // an invite token rather than a meetingId, and are used by invitees who are not
    // necessarily authenticated project members (e.g. guests following an email link).
    [ApiController]
    [Route("api/meeting-invitations")]
    public class MeetingInvitationsController(IMeetingService _meetingService) : ControllerBase
    {
        [HttpGet("{inviteToken}")]
        public async Task<IActionResult> PreviewInvitation([FromRoute] string inviteToken, CancellationToken cancellationToken)
        {
            var response = await _meetingService.PreviewInvitationAsync(inviteToken, cancellationToken);

            return response.StatusCode switch
            {
                200 => Ok(response),
                404 => NotFound(response),
                422 => UnprocessableEntity(response),
                500 => StatusCode(500, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        [HttpPost("{inviteToken}/accept")]
        public async Task<IActionResult> AcceptInvitation([FromRoute] string inviteToken, CancellationToken cancellationToken)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(Response<AcceptMeetingInvitationResponse>.Failure(null, "Unauthorized User", 401));

            var response = await _meetingService.AcceptInvitationAsync(inviteToken, userId, cancellationToken);

            return response.StatusCode switch
            {
                200 => Ok(response),
                400 => BadRequest(response),
                401 => Unauthorized(response),
                403 => StatusCode(403, response),
                404 => NotFound(response),
                409 => Conflict(response),
                422 => UnprocessableEntity(response),
                500 => StatusCode(500, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }
    }
}