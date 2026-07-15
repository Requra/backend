using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.Interfaces.IRecordingService
{
    public interface IRecordingFinalizationService
    {
        Task FinalizeRecordingAsync(Guid recordingId, CancellationToken cancellationToken = default);
    }
}
