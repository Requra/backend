using Microsoft.AspNetCore.Http;
using Requra.Application.DTOs.AI;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.Interfaces.IAnalysisRunWorker
{
    public interface IAnalysisRunWorker
    {
        //Task ProcessRun(Guid runId, Guid projectId, List<Guid> documentIds);
        Task ProcessRun(List<FileUploadDto> files, Guid runId, Guid projectId);

    }
}
