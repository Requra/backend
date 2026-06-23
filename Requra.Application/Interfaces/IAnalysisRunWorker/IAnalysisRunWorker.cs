using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.Interfaces.IAnalysisRunWorker
{
    public interface IAnalysisRunWorker
    {
        Task ProcessRun(Guid runId, Guid projectId, List<Guid> documentIds);
    }
}
