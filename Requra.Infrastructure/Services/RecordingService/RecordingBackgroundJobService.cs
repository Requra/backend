using Hangfire;
using Requra.Application.Interfaces.IRecordingService;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.Services.RecordingService
{
    public class RecordingBackgroundJobService : IRecordingBackgroundJobService
    {
        public Task EnqueueFinalizeRecordingAsync(Guid recordingId, CancellationToken cancellationToken = default)
        {
            BackgroundJob.Enqueue<IRecordingFinalizationService>(
                service => service.FinalizeRecordingAsync(recordingId, CancellationToken.None));

            return Task.CompletedTask;
        }
    }
}
