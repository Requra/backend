using Requra.Application.DTOs;
using Requra.Application.DTOs.Project;
using Requra.Application.DTOs.Project.ProjectCreation;
using Requra.Application.Response;
using Requra.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.Interfaces.IProjectService
{
    public interface IProjectService
    {
        Task<Response<PagedResult<ProjectDTO>>> GetUserProjectsAsync(ProjectFilter filter);

        Task<Response<ProjectResponseDto>> CreateProjectAsync(ProjectRequestDto request, string currentUserId);
    }
}
