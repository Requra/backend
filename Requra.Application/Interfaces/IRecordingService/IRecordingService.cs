using Requra.Application.DTOs.Document;
using Requra.Application.DTOs.Recordings;
using Requra.Application.Response;
using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Application.Interfaces.IRecordingService
{
    public interface IRecordingService
    {
        Task<Response<StartRecordingResponse>> StartRecordingAsync(StartRecordingRequest request,CancellationToken cancellationToken = default);

        Task<Response<UploadChunkResponse>> UploadChunkAsync(UploadChunkRequest request,CancellationToken cancellationToken = default);

        Task<Response<UploadRecordingFileResponse>> UploadRecordingFileAsync(UploadRecordingFileRequest request,CancellationToken cancellationToken = default);

        Task<Response<StopRecordingResponse>> StopRecordingAsync(StopRecordingRequest request,CancellationToken cancellationToken = default);
    }
}
