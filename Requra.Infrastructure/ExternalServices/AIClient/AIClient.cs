using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Requra.Application.DTOs.AI;
using Requra.Application.Interfaces.IAIService;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using static Requra.Infrastructure.Services.AnalysisRunService.AnalysisRunService;

namespace Requra.Infrastructure.ExternalServices.AIClient
{
    public class AIClient : IAIClient
    {
        private readonly HttpClient _httpClient;

        public AIClient(HttpClient httpClient,IConfiguration config)
        {
            _httpClient = httpClient;
            var token = config["AI:ServiceToken"];
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

        }

        //public async Task<JobStatusResponseDto> GetStatusAsync(string jobId)
        //{
        //    var response = await _httpClient.GetAsync($"/status/{jobId}");

        //    response.EnsureSuccessStatusCode();

        //    var result = await response.Content.ReadFromJsonAsync<JobStatusResponseDto>();

        //    return result!;
        
        //}

        //public async Task<ProcessJsonResponse> ProcessAsync(ProcessJsonRequest request)
        //{
        //    //var response = await _httpClient.PostAsJsonAsync("/process-json", request);
        //    //var content = await response.Content.ReadAsStringAsync();
        //    //Console.WriteLine(content);
        //    //response.EnsureSuccessStatusCode();

        //    //return await response.Content.ReadFromJsonAsync<ProcessJsonResponse>();
        //    request.JobId ??= Guid.NewGuid().ToString();

        //    var response = await _httpClient.PostAsJsonAsync("/process-json", request);

        //    response.EnsureSuccessStatusCode();

        //    var result = await response.Content.ReadFromJsonAsync<ProcessJsonResponse>();

        //    return result!;
        //}
        public async Task<string> ProcessAsync(ProcessJsonRequest request)
        {
            request.JobId ??= Guid.NewGuid().ToString(); //optional step

            var response = await _httpClient.PostAsJsonAsync("/process-json", request);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ProcessJsonResponse>();

            Console.WriteLine($"JobId: {result?.JobId}, Status: {result?.Status}");

            return result?.JobId.ToString() ?? string.Empty;
        }
        public async Task<string> ProcessSingleFileAsync(byte[] bytes,string fileName,string contentType,string metadataJson)
{
    using var form = new MultipartFormDataContent();

    var fileContent = new ByteArrayContent(bytes);
    fileContent.Headers.ContentType =
        new MediaTypeHeaderValue(contentType);

    form.Add(fileContent, "file", fileName);

    form.Add(new StringContent(metadataJson, Encoding.UTF8, "application/json"), "metadata");

    var response = await _httpClient.PostAsync("/process", form);

    if (!response.IsSuccessStatusCode)
    {
        var error = await response.Content.ReadAsStringAsync();
        throw new Exception($"AI Error: {error}");
    }

    var result = await response.Content.ReadFromJsonAsync<ProcessJsonResponse>();
    return result?.JobId.ToString() ?? string.Empty;
}

        public async Task<ProcessResponseDto> SubmitAsync(List<FileUploadDto> files, string projectId, string? jobId)
        {
            var content = new MultipartFormDataContent();

            //foreach (var file in files)
            //{
            //    content.Add(new StreamContent(file.OpenReadStream()), "files", file.FileName);
            //}
            foreach (var file in files)
            {
                var stream = new MemoryStream(file.Content);

                var streamContent = new StreamContent(stream);
                streamContent.Headers.ContentType =
                    new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

                content.Add(streamContent, "files", file.FileName);
            }

            content.Add(new StringContent(projectId), "project_id");

            if (!string.IsNullOrEmpty(jobId))
                content.Add(new StringContent(jobId), "job_id");

            var response = await _httpClient.PostAsync("internal/process", content);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<ProcessResponseDto>();
        }

        public async Task<JobStatusResponseDto> GetStatusAsync(string jobId)
        {
            var response = await _httpClient.GetAsync($"internal/jobs/{jobId}");
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<JobStatusResponseDto>();
        }

        public async Task<JobResultResponseDto> GetResultAsync(string jobId)
        {
            var response = await _httpClient.GetAsync($"internal/jobs/{jobId}/result");
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<JobResultResponseDto>();
        }

        public async Task<CancelJobResponseDto> CancelJobAsync(string jobId)
        {
            var response = await _httpClient.PostAsync($"internal/jobs/{jobId}/cancel", null);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<CancelJobResponseDto>();
        }

        public async Task<RetryJobResponseDto> RetryJobAsync(string jobId)
        {
            var response = await _httpClient.PostAsync($"internal/jobs/{jobId}/retry", null);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<RetryJobResponseDto>();
        }
    }

    }
