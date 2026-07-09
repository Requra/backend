using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Requra.Application.DTOs;
using Requra.Application.DTOs.Meeting;
using Requra.Application.DTOs.ProjectMembers;
using Requra.Application.Interfaces.IMeetingService;
using Requra.Application.Response;
using System.Security.Claims;

namespace Requra.Presentation.Controllers.Meeting
{
    [ApiController]
    [Route("api/projects/{projectId}/meetings")]
    public class MeetingsController(IMeetingService _meetingService) : ControllerBase
    {
       
        [HttpPost]
        public async Task<IActionResult> CreateMeeting(
            Guid projectId,
            [FromBody] CreateMeetingRequest request)
        {
            //var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            //if (string.IsNullOrEmpty(userId))
            //    return Unauthorized(Response<MeetingDto>.Failure(null, "Unauthorized User", 401));

            var userId = "01f535f1-9870-4141-9b29-21df2d9cd6ec"; // Hardcoded userId for testing purposes
            if (projectId == Guid.Empty || !Guid.TryParse(projectId.ToString(), out Guid validGuid))
                return StatusCode(422, Response<MeetingDto>.Failure(null, "Validation failed", 422, new List<string> { "Invalid projectId" }));

            var result = await _meetingService
                .CreateMeetingAsync(projectId, request, userId);

            return StatusCode(result.StatusCode, result);
        }
    }
}
