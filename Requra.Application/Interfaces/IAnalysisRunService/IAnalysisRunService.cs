using Requra.Application.DTOs.AI;
using Requra.Application.Response;
using Requra.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.Interfaces.IAnalysisRunService
{
    public interface IAnalysisRunService
    {
        Task<Response<AnalysisRunDto>> StartRunAsync(Guid projectId, StartRunRequest request);
        Task<Response<AnalysisRunDto>> GetRunAsync(Guid projectId, Guid runId);
        //Task<Response<ResultsDashboardDto>> GetResultAsync(Guid runId);
        Task<Response<ResultDto>> GetResultAsync(Guid projectId, Guid runId);

    }
}
