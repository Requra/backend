using Requra.Application.DTOs;
using Requra.Application.DTOs.Project.ProjectResults.UserStory;
using Requra.Application.Response;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.Interfaces.IProjectService.IProjectResultsService.IUserStoryService
{
    public interface IUserStoryService
    {
        Task<Response<PagedResult<UserStoryDto>>> GetUserStoriesByProjectIdAsync(Guid projectId);
        Task<Response<UpdateUserStoryStatusResponse>> UpdateUserStoryStatusAsync(UpdateUserStoryStatusRequest request, CancellationToken cancellationToken = default);
        Task<Response<EditUserStoryContentResponse>> EditUserStoryContentAsync(EditUserStoryContentRequest request, CancellationToken cancellationToken = default);
    }
}
