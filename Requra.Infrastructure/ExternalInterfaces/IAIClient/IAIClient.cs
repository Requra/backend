using Microsoft.AspNetCore.Http;
using Requra.Application.DTOs.AI;
using System;
using System.Collections.Generic;
using System.Text;
using static Requra.Infrastructure.Services.AnalysisRunService.AnalysisRunService;

namespace Requra.Application.Interfaces.IAIService
{
    public interface IAIClient
    {
        //Task<ProcessJsonResponse> ProcessAsync(ProcessJsonRequest request);
        Task<string> ProcessAsync(ProcessJsonRequest request);
        Task<string> ProcessSingleFileAsync(
    byte[] bytes,
    string fileName,
    string contentType,
    string metadataJson);
        



       Task<JobStatusResponseDto> GetStatusAsync(string jobId);

        Task<ProcessResponseDto> SubmitAsync(List<FileUploadDto> files, string projectId, string? jobId);
        Task<JobResultResponseDto> GetResultAsync(string jobId);

        Task<CancelJobResponseDto> CancelJobAsync(string jobId);

        Task<RetryJobResponseDto> RetryJobAsync(string jobId);

    }
}
