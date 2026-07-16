using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Requra.Application.DTOs;
using Requra.Application.DTOs.Meeting;
using Requra.Application.DTOs.Project.ProjectDetails;
using Requra.Application.DTOs.ProjectMembers;
using Requra.Application.Interfaces.IMeetingService;
using Requra.Application.Response;
using Requra.Domain.Entities;
using Requra.Infrastructure.Services.MeetingService;
using System.Security.Claims;

namespace Requra.Presentation.Controllers.Meeting
{
    [ApiController]
    [Route("api/[controller]")]
    public class MeetingsController(IMeetingService _meetingService) : ControllerBase
    {
        [HttpGet("{meetingId}")]
        public async Task<IActionResult> GetMeeting(string meetingId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(Response<MeetingDetailsDto>.Failure(null, "Unauthorized User", 401));

            //var userId = "01f535f1-9870-4141-9b29-21df2d9cd6ec"; // Hardcoded userId for testing purposes

            if (!Guid.TryParse(meetingId, out var meetingguid))
            {
                return StatusCode(422, Response<MeetingDetailsDto>.Failure(null, "Validation failed", 422, new List<string> { "Invalid meetingId format" }));

            }
            var result = await _meetingService
                .GetMeetingByIdAsync(meetingguid, userId);

            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("{meetingId}/cancel")]
        public async Task<IActionResult> CancelMeeting(string meetingId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(Response<MeetingDto>.Failure(null, "Unauthorized User", 401));

            //var userId = "01f535f1-9870-4141-9b29-21df2d9cd6ec"; // Hardcoded userId for testing purposes

            if (!Guid.TryParse(meetingId, out var meetingguid))
            {
                return StatusCode(422, Response<MeetingDto>.Failure(null, "Validation failed", 422, new List<string> { "Invalid meetingId format" }));

            }

            var result = await _meetingService
                .CancelMeetingAsync(meetingguid, userId);

            return StatusCode(result.StatusCode, result);
        }

        [HttpPatch("{meetingId}")]
        public async Task<IActionResult> UpdateMeeting(string meetingId,[FromBody] UpdateMeetingRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(Response<MeetingDto>.Failure(null, "Unauthorized User", 401));
            //var userId = "01f535f1-9870-4141-9b29-21df2d9cd6ec"; // Hardcoded userId for testing purposes
            if (!Guid.TryParse(meetingId, out var meetingguid))
            {
                return StatusCode(422, Response<MeetingDto>.Failure(null, "Validation failed", 422, new List<string> { "Invalid meetingId format" }));

            }
            var result = await _meetingService
                .UpdateMeetingAsync(meetingguid, request, userId);

            return StatusCode(result.StatusCode, result);
        }




        [HttpPost("{meetingId:guid}/start")]
        public async Task<IActionResult> StartMeeting([FromRoute] Guid meetingId,CancellationToken cancellationToken)
        {
            var response = await _meetingService.StartMeetingAsync(meetingId, cancellationToken);

            return response.StatusCode switch
            {
                200 => Ok(response),
                201 => StatusCode(201, response),
                204 => NoContent(),
                400 => BadRequest(response),
                404 => NotFound(response),
                409 => Conflict(response),
                500 => StatusCode(500, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        [HttpPost("{meetingId:guid}/end")]
        public async Task<IActionResult> EndMeeting(
        [FromRoute] Guid meetingId,
        CancellationToken cancellationToken)
        {
            var response = await _meetingService.EndMeetingAsync(meetingId, cancellationToken);

            return response.StatusCode switch
            {
                200 => Ok(response),
                201 => StatusCode(201, response),
                204 => NoContent(),
                400 => BadRequest(response),
                404 => NotFound(response),
                409 => Conflict(response),
                500 => StatusCode(500, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

    }
}
