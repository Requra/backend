using Requra.Application.DTOs;
using Requra.Application.DTOs.Invitation.MeetingInvitation;
using Requra.Application.DTOs.Meeting;
using Requra.Application.Response;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.Interfaces.IMeetingService
{
    public interface IMeetingService
    {
        Task<Response<MeetingDto>> CreateMeetingAsync(
            Guid projectId,
            CreateMeetingRequest request,
            string currentUserId);
        Task<Response<PagedResult<ProjectMeetingsDto>>> GetMeetingsAsync(
        Guid projectId,
        string currentUserId,
        GetMeetingsQuery query);

        Task<Response<MeetingDetailsDto>> GetMeetingByIdAsync(Guid meetingId,string currentUserId);
        Task<Response<MeetingDto>> CancelMeetingAsync(Guid meetingId,string currentUserId);
        Task<Response<MeetingDto>> UpdateMeetingAsync(Guid meetingId,UpdateMeetingRequest request,string currentUserId);


        Task<Response<StartMeetingResponse>> StartMeetingAsync(Guid MeetingId, CancellationToken cancellationToken = default);
        Task<Response<EndMeetingResponse>> EndMeetingAsync(Guid MeetingId, CancellationToken cancellationToken = default);
        Task<Response<InviteMeetingParticipantsResponse>> InviteParticipantsAsync(InviteMeetingParticipantsRequest request, CancellationToken cancellationToken = default);
        Task<Response<InviteGuestsResponse>> InviteGuestsAsync(InviteGuestsRequest request, CancellationToken cancellationToken = default);
    }
}
