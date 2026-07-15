using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.Interfaces.IRecordingService
{
    public interface IRecordingChunkStorageReader
    {
        Task<Stream> OpenReadAsync(string storageUrl, string? publicId, CancellationToken cancellationToken = default);
    }
}
