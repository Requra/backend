using Requra.Application.DTOs.AI;
using Requra.Application.Interfaces.IAIService;
using System;
using System.Collections.Generic;
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

        public async Task<ProcessJsonResponse> ProcessAsync(ProcessJsonRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("/process-json", request);

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<ProcessJsonResponse>();
        }
    }
}
