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
        Task<Response<AnalysisRunDto>> StartRunAsync(Guid projectId, List<Guid> documentIds, Guid? meetingId);
        Task<AnalysisRun> GetRunAsync(Guid runId);
        Task<AnalysisResult> GetResultAsync(Guid runId);
    }
}
