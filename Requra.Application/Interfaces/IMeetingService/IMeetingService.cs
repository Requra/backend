using Requra.Application.DTOs;
using Requra.Application.DTOs.Invitation.MeetingInvitation;
using Requra.Application.DTOs.LiveKit;
using Requra.Application.DTOs.Meeting;
using Requra.Application.DTOs.Participant;
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
        //invitations
        Task<Response<PagedResult<MeetingInvitationItemResponse>>> GetMeetingInvitationsAsync(Guid meetingId, string currentUserId, GetMeetingInvitationsQuery query, CancellationToken cancellationToken = default);
        Task<Response<MeetingInvitationPreviewResponse>> PreviewInvitationAsync(string inviteToken, CancellationToken cancellationToken = default);
        Task<Response<AcceptMeetingInvitationResponse>> AcceptInvitationAsync(string inviteToken, string currentUserId, CancellationToken cancellationToken = default);
        Task<Response<MeetingInvitationDetailResponse>> ResendInvitationAsync(Guid meetingId, Guid invitationId, string currentUserId, CancellationToken cancellationToken = default);
        Task<Response<MeetingInvitationDetailResponse>> RevokeInvitationAsync(Guid meetingId, Guid invitationId, string currentUserId, CancellationToken cancellationToken = default);
        //participants
        Task<Response<MeetingParticipantResponse>> JoinMeetingAsync(JoinMeetingRequest request, CancellationToken cancellationToken = default);
        Task<Response<MeetingParticipantResponse>> LeaveMeetingAsync(LeaveMeetingRequest request, CancellationToken cancellationToken = default);
        Task<Response<PagedResult<MeetingParticipantResponse>>> GetMeetingParticipantsAsync(Guid meetingId, string currentUserId, GetMeetingParticipantsQuery query, CancellationToken cancellationToken = default);
        Task<Response<MeetingParticipantResponse>> RemoveParticipantAsync(RemoveParticipantRequest request, CancellationToken cancellationToken = default);
        Task<Response<MeetingParticipantResponse>> SaveConsentAsync(SaveConsentRequest request, CancellationToken cancellationToken = default);



        Task<Response<LiveKitTokenResponseDto>> IssueTokenAsync(Guid meetingId,string callerUserId,Guid? participantId,CancellationToken cancellationToken = default);

    }
}
