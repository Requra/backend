using Requra.Application.DTOs;
using Requra.Application.DTOs.Project.ProjectResults.Feedbacks;
using Requra.Application.DTOs.ProjectReviewInvitaion;
using Requra.Application.Response;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.Interfaces.IProjectService.IProjectReviewService
{
    public interface IProjectReviewService
    {
        Task<Response<SubmitStakeholderFeedbackResponse>> SubmitStakeholderFeedbackAsync(SubmitStakeholderFeedbackRequest request, CancellationToken cancellationToken = default);
        Task<Response<ListStakeholderFeedbackResponse>> ListStakeholderFeedbackAsync(ListStakeholderFeedbackRequest request, CancellationToken cancellationToken = default);
        Task<Response<SubmitStakeholderFeedbackResponse>> UpdateStakeholderFeedbackStatusAsync(UpdateStakeholderFeedbackStatusRequest request, CancellationToken cancellationToken = default);
        Task<Response<ListStakeholderFeedbackResponse>> ListProjectStakeholderFeedbackAsync(ListProjectStakeholderFeedbackRequest request, CancellationToken cancellationToken = default);
        Task<Response<List<ProjectReviewInvitationDto>>> CreateInvitationAsync(Guid projectId,CreateProjectReviewInvitationRequest request,string userId);
        Task<Response<ProjectReviewInvitationsPagedResult<ProjectReviewInvitationDto>>> GetProjectReviewInvitationsAsync(
        Guid projectId,
        GetProjectReviewInvitationsQuery query,
        string userId);

        Task<Response<ProjectReviewInvitationDto>> ResendInvitationAsync(Guid projectId, Guid invitationId, string ResendByUserId);
        Task<Response<RevokeInvitationResponseDto>> RevokeInvitationAsync(Guid projectId, Guid invitationId, string userId);
        Task<Response<PreviewProjectReviewInvitationResponse>> PreviewProjectReviewInvitationAsync(PreviewProjectReviewInvitationRequest request, CancellationToken cancellationToken = default);
        Task<Response<AcceptProjectReviewInvitationResponse>> AcceptProjectReviewInvitationAsync(AcceptProjectReviewInvitationRequest request, CancellationToken cancellationToken = default);
        Task<Response<GetProjectReviewDashboardResponse>> GetProjectReviewDashboardAsync(GetProjectReviewDashboardRequest request, CancellationToken cancellationToken = default);
    }
}
