using Requra.Application.DTOs.Project.Requirements;
using Requra.Application.Response;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.Interfaces.IProjectService.IRequirementService
{
    public interface IRequirementService
    {
        Task<Response<UpdateRequirementStatusResponse>> UpdateRequirementStatusAsync(UpdateRequirementStatusRequest request, CancellationToken cancellationToken = default);
    }
}
