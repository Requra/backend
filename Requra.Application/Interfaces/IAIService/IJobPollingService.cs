using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.Interfaces.IAIService
{
    public interface IJobPollingService
    {
        Task PollUntilFinishedAsync(Guid runId, string jobId, int maxAttempts = 1000);
    }
}
