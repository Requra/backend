using Microsoft.AspNetCore.Http;
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
        Task<Response<AnalysisRunDto>> StartRunAsync(StartRunRequest request,Guid projectId, string userId);
        Task<Response<AnalysisRunDto>> GetRunAsync(Guid projectId, Guid runId,string userId);
        //Task<Response<ResultsDashboardDto>> GetResultAsync(Guid runId);
        Task<Response<ExportsDto>> GetResultAsync(Guid projectId, Guid? runId, string userId);
        Task<Response<CancelJobResponseDto>> CancelRunAsync(Guid projectId, Guid runId, string userId);
        Task<Response<RetryJobResponseDto>> RetryRunAsync(Guid projectId, Guid runId, string userId);
    }
}
