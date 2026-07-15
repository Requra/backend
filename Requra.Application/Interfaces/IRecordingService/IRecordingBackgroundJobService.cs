using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.Interfaces.IRecordingService
{
    public interface IRecordingBackgroundJobService
    {
        Task EnqueueFinalizeRecordingAsync(Guid recordingId, CancellationToken cancellationToken = default);
    }
}
