using Requra.Application.DTOs.Project.ProjectResults.Feedbacks;
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
    }
}
