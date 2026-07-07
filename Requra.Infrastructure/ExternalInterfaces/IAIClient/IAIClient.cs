using Requra.Application.DTOs.AI;
using System;
using System.Collections.Generic;
using System.Text;

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
        
    }
}
