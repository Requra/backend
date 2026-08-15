using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Requra.Application.DTOs;
using Requra.Application.DTOs.Agora;
using Requra.Application.DTOs.Invitation.MeetingInvitation;
using Requra.Application.DTOs.LiveKit;
using Requra.Application.DTOs.Meeting;
using Requra.Application.DTOs.Participant;
using Requra.Application.DTOs.Project.ProjectDetails;
using Requra.Application.DTOs.ProjectMembers;
using Requra.Application.Interfaces.IMeetingService;
using Requra.Application.Response;
using Requra.Domain.Entities;
using Requra.Infrastructure.Services.MeetingService;
using System.Security.Claims;
using static Google.Apis.Requests.BatchRequest;

namespace Requra.Presentation.Controllers.Meeting
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MeetingsController(IMeetingService _meetingService) : ControllerBase
    {
        [HttpGet("{meetingId}")]
        public async Task<IActionResult> GetMeeting(string meetingId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(Response<MeetingDetailsDto>.Failure(new MeetingDetailsDto(), "Unauthorized User", 401));

            //var userId = "01f535f1-9870-4141-9b29-21df2d9cd6ec"; // Hardcoded userId for testing purposes

            if (!Guid.TryParse(meetingId, out var meetingguid))
            {
                return StatusCode(422, Response<MeetingDetailsDto>.Failure(new MeetingDetailsDto(), "Validation failed", 422, new List<string> { "Invalid meetingId format" }));

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
                return Unauthorized(Response<MeetingDto>.Failure(new MeetingDto(), "Unauthorized User", 401));

            //var userId = "01f535f1-9870-4141-9b29-21df2d9cd6ec"; // Hardcoded userId for testing purposes

            if (!Guid.TryParse(meetingId, out var meetingguid))
            {
                return StatusCode(422, Response<MeetingDto>.Failure(new MeetingDto(), "Validation failed", 422, new List<string> { "Invalid meetingId format" }));

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
                return Unauthorized(Response<MeetingDto>.Failure(new MeetingDto(), "Unauthorized User", 401));
            //var userId = "01f535f1-9870-4141-9b29-21df2d9cd6ec"; // Hardcoded userId for testing purposes
            if (!Guid.TryParse(meetingId, out var meetingguid))
            {
                return StatusCode(422, Response<MeetingDto>.Failure(new MeetingDto(), "Validation failed", 422, new List<string> { "Invalid meetingId format" }));

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
        public async Task<IActionResult> EndMeeting([FromRoute] Guid meetingId,CancellationToken cancellationToken)
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

       
        
        [HttpPost("{meetingId:guid}/invitations/participants")]
        public async Task<IActionResult> InviteParticipants([FromRoute] Guid meetingId,[FromBody] InviteMeetingParticipantsApiRequest request,CancellationToken cancellationToken)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var requestdto = new InviteMeetingParticipantsRequest
            {
                MeetingId = meetingId,
                Members = request.Members,
                InvitedById = userId
            };

            var response = await _meetingService.InviteParticipantsAsync(requestdto, cancellationToken);

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

        [HttpPost("{meetingId:guid}/invitations/guests")]
        public async Task<IActionResult> InviteGuests([FromRoute] Guid meetingId,[FromBody] InviteGuestsApiRequest request,CancellationToken cancellationToken)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var requestdto = new InviteGuestsRequest
            {
                MeetingId = meetingId,
                Guests = request.Guests,
                InvitedById = userId
            };

            var response = await _meetingService.InviteGuestsAsync(requestdto, cancellationToken);

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
       
        
        
        
        [HttpGet("{meetingId:guid}/invitations")]
        public async Task<IActionResult> GetInvitations([FromRoute] Guid meetingId, [FromQuery] GetMeetingInvitationsQuery query, CancellationToken cancellationToken)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(Response<PagedResult<MeetingInvitationItemResponse>>.Failure(null, "Unauthorized User", 401));

            var response = await _meetingService.GetMeetingInvitationsAsync(meetingId, userId, query, cancellationToken);

            return response.StatusCode switch
            {
                200 => Ok(response),
                400 => BadRequest(response),
                401 => Unauthorized(response),
                403 => StatusCode(403, response),
                404 => NotFound(response),
                422 => UnprocessableEntity(response),
                500 => StatusCode(500, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        [HttpPost("{meetingId:guid}/invitations/{invitationId:guid}/resend")]
        public async Task<IActionResult> ResendInvitation([FromRoute] Guid meetingId, [FromRoute] Guid invitationId, CancellationToken cancellationToken)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(Response<MeetingInvitationDetailResponse>.Failure(null, "Unauthorized User", 401));

            var response = await _meetingService.ResendInvitationAsync(meetingId, invitationId, userId, cancellationToken);

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

        [HttpDelete("{meetingId:guid}/invitations/{invitationId:guid}")]
        public async Task<IActionResult> RevokeInvitation([FromRoute] Guid meetingId, [FromRoute] Guid invitationId, CancellationToken cancellationToken)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(Response<MeetingInvitationDetailResponse>.Failure(null, "Unauthorized User", 401));

            var response = await _meetingService.RevokeInvitationAsync(meetingId, invitationId, userId, cancellationToken);

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
        [HttpPost("{meetingId:guid}/join")]
        public async Task<IActionResult> JoinMeeting([FromRoute] Guid meetingId, [FromBody] JoinMeetingApiRequest request, CancellationToken cancellationToken)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var requestDto = new JoinMeetingRequest
            {
                MeetingId = meetingId,
                CurrentUserId = string.IsNullOrEmpty(userId) ? null : userId,
                DisplayName = request?.DisplayName,
                Email = request?.Email
            };

            var response = await _meetingService.JoinMeetingAsync(requestDto, cancellationToken);

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

        [HttpPost("{meetingId:guid}/leave")]
        public async Task<IActionResult> LeaveMeeting([FromRoute] Guid meetingId, [FromBody] LeaveMeetingApiRequest request, CancellationToken cancellationToken)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var requestDto = new LeaveMeetingRequest
            {
                MeetingId = meetingId,
                CurrentUserId = string.IsNullOrEmpty(userId) ? null : userId,
                ParticipantId = request?.ParticipantId
            };

            var response = await _meetingService.LeaveMeetingAsync(requestDto, cancellationToken);

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

        [HttpGet("{meetingId:guid}/participants")]
        public async Task<IActionResult> GetParticipants([FromRoute] Guid meetingId, [FromQuery] GetMeetingParticipantsQuery query, CancellationToken cancellationToken)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(Response<PagedResult<MeetingParticipantResponse>>.Failure(null, "Unauthorized User", 401));

            var response = await _meetingService.GetMeetingParticipantsAsync(meetingId, userId, query, cancellationToken);

            return response.StatusCode switch
            {
                200 => Ok(response),
                400 => BadRequest(response),
                401 => Unauthorized(response),
                403 => StatusCode(403, response),
                404 => NotFound(response),
                422 => UnprocessableEntity(response),
                500 => StatusCode(500, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        [HttpDelete("{meetingId:guid}/participants/{participantId:guid}")]
        public async Task<IActionResult> RemoveParticipant([FromRoute] Guid meetingId, [FromRoute] Guid participantId, CancellationToken cancellationToken)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(Response<MeetingParticipantResponse>.Failure(null, "Unauthorized User", 401));

            var requestDto = new RemoveParticipantRequest
            {
                MeetingId = meetingId,
                ParticipantId = participantId,
                CurrentUserId = userId
            };

            var response = await _meetingService.RemoveParticipantAsync(requestDto, cancellationToken);

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

        [HttpPost("{meetingId:guid}/participants/{participantId:guid}/consent")]
        public async Task<IActionResult> SaveConsent([FromRoute] Guid meetingId, [FromRoute] Guid participantId, [FromBody] SaveConsentApiRequest request, CancellationToken cancellationToken)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var requestDto = new SaveConsentRequest
            {
                MeetingId = meetingId,
                ParticipantId = participantId,
                CurrentUserId = string.IsNullOrEmpty(userId) ? null : userId,
                RecordingConsent = request?.RecordingConsent ?? false
            };

            var response = await _meetingService.SaveConsentAsync(requestDto, cancellationToken);

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



        [HttpPost("{meetingId:guid}/livekit-token")]
        public async Task<IActionResult> IssueLiveKitToken([FromRoute] Guid meetingId,[FromBody] LiveKitTokenRequestDto request,CancellationToken cancellationToken)
        {

            var callerUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var result = await _meetingService.IssueTokenAsync(meetingId,callerUserId!,request?.ParticipantId,cancellationToken);

            return result.StatusCode switch
            {
                200 => Ok(result),
                400 => BadRequest(result),
                401 => Unauthorized(result),
                403 => StatusCode(403, result),
                404 => NotFound(result),
                422 => UnprocessableEntity(result),
                500 => StatusCode(500, result),
                _ => StatusCode(result.StatusCode, result)
            };

           
        }
        [HttpPost("{meetingId:guid}/agora-token")]
        
        public async Task<IActionResult> IssueAgoraToken([FromRoute] Guid meetingId,[FromQuery] Guid? participantId,CancellationToken cancellationToken)
        {
            var callerUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);


            var response = await _meetingService.IssueAgoraTokenAsync( meetingId, callerUserId, participantId, cancellationToken);

            return response.StatusCode switch
            {
                200 => Ok(response),
                400 => BadRequest(response),
                401 => Unauthorized(response),
                403 => StatusCode(403, response),
                404 => NotFound(response),
                422 => UnprocessableEntity(response),
                500 => StatusCode(500, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

    }
}
