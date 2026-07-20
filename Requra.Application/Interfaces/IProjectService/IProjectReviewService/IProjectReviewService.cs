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
        Task<Response<List<ProjectReviewInvitationDto>>> CreateInvitationAsync(string projectId,CreateProjectReviewInvitationRequest request,string userId);
        Task<Response<ProjectReviewInvitationsPagedResult<ProjectReviewInvitationDto>>> GetProjectReviewInvitationsAsync(
        string projectId,
        GetProjectReviewInvitationsQuery query,
        string userId);
    }
}
