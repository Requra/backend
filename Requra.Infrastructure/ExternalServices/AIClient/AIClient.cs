using Requra.Application.DTOs.AI;
using Requra.Application.Interfaces.IAIService;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;

namespace Requra.Infrastructure.ExternalServices.AIClient
{
    public class AIClient : IAIClient
    {
        private readonly HttpClient _httpClient;

        public AIClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<JobStatusResponseDto> GetStatusAsync(string jobId)
        {
            var response = await _httpClient.GetAsync($"/status/{jobId}");

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<JobStatusResponseDto>();

            return result!;
        
        }

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
        public async Task<string> ProcessSingleFileAsync(
    byte[] bytes,
    string fileName,
    string contentType,
    string metadataJson)
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
    }
}
