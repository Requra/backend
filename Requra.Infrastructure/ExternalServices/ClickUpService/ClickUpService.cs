using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Requra.Infrastructure.ExternalDTOs.ClickUpDto;
using Requra.Infrastructure.ExternalInterfaces.IClickUpService;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Requra.Infrastructure.ExternalServices.ClickUpService
{
    public class ClickUpService : IClickUpService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ClickUpService> _logger;

        private const string ApiBaseUrl = "https://api.clickup.com/api/v2";
        private const string AuthBaseUrl = "https://app.clickup.com/api";
        private const string TokenEndpoint = "https://api.clickup.com/api/v2/oauth/token";


        public ClickUpService(HttpClient httpClient, IConfiguration configuration, ILogger<ClickUpService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;

            // Set base address for API calls
            _httpClient.BaseAddress = new Uri(ApiBaseUrl);
        }

        public string GetAuthorizationUrl(string redirectUri)
        {
            var clientId = _configuration["ClickUp:ClientId"]
                ?? throw new InvalidOperationException("ClickUp ClientId not configured");

            var authUrl = $"{AuthBaseUrl}?client_id={clientId}&redirect_uri={Uri.EscapeDataString(redirectUri)}";
            _logger.LogInformation("Generated ClickUp authorization URL: {AuthUrl}", authUrl);
            return authUrl;
        }

        public async Task<ClickUpTokenResponse> ExchangeAuthorizationCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            try
            {
                var clientId = _configuration["ClickUp:ClientId"]
                    ?? throw new InvalidOperationException("ClickUp ClientId not configured");
                var clientSecret = _configuration["ClickUp:ClientSecret"]
                    ?? throw new InvalidOperationException("ClickUp ClientSecret not configured");

                var requestBody = new
                {
                    client_id = clientId,
                    client_secret = clientSecret,
                    code  
                };

                _logger.LogInformation("Attempting to exchange authorization code for access token. TokenUrl: {TokenUrl}, Code: {Code}, ClientId: {ClientId}", 
                    TokenEndpoint, code, clientId);

                var response = await _httpClient.PostAsJsonAsync(
                    TokenEndpoint,
                    requestBody,
                    cancellationToken);

                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("ClickUp token exchange failed. Status: {StatusCode}, Response: {Response}", 
                        response.StatusCode, responseContent);
                    throw new HttpRequestException($"Token exchange failed: {response.StatusCode} - {responseContent}");
                }

                var tokenResponse = JsonSerializer.Deserialize<ClickUpTokenResponse>(responseContent, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (tokenResponse == null)
                {
                    _logger.LogError("Failed to deserialize token response: {Response}", responseContent);
                    throw new InvalidOperationException("Failed to deserialize token response from ClickUp");
                }

                _logger.LogInformation("Successfully exchanged authorization code for access token. ExpiresIn: {ExpiresIn} seconds (~{Hours} hours)", 
                    tokenResponse.ExpiresIn, tokenResponse.ExpiresIn / 3600);
                return tokenResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to exchange authorization code for access token");
                throw;
            }
        }

        public async Task<ClickUpWorkspaceResponse> GetAuthorizedUserAsync(string accessToken, CancellationToken cancellationToken = default)
        {
            try
            {
                // Use absolute URI to ensure correct endpoint
                var userEndpoint = "https://api.clickup.com/api/v2/user";
                var request = new HttpRequestMessage(HttpMethod.Get, userEndpoint);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                var response = await _httpClient.SendAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();

                var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogInformation("ClickUp user response: {Response}", jsonContent);

                var result = System.Text.Json.JsonSerializer.Deserialize<ClickUpWorkspaceResponse>(jsonContent, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                _logger.LogInformation("Retrieved authorized user workspace information for user: {Username}", result?.User?.Username);
                return result ?? throw new InvalidOperationException("Failed to deserialize user response");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to retrieve authorized user information");
                throw;
            }
        }

        public async Task<ClickUpTeamsResponse> GetUserTeamsAsync(string accessToken, CancellationToken cancellationToken = default)
        {
            try
            {
                var teamsEndpoint = "https://api.clickup.com/api/v2/team";
                var request = new HttpRequestMessage(HttpMethod.Get, teamsEndpoint);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                var response = await _httpClient.SendAsync(request, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("Failed to get teams. Status: {StatusCode}, Response: {Response}", response.StatusCode, errorContent);
                    throw new HttpRequestException($"Failed to get teams: {response.StatusCode} - {errorContent}");
                }

                var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogInformation("ClickUp teams response: {Response}", jsonContent);

                var result = System.Text.Json.JsonSerializer.Deserialize<ClickUpTeamsResponse>(jsonContent, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (result == null)
                {
                    _logger.LogError("Failed to deserialize teams response");
                    throw new InvalidOperationException("Failed to deserialize teams response");
                }

                _logger.LogInformation("Retrieved user teams. Teams count: {TeamsCount}", result.Teams?.Count ?? 0);
                if (result.Teams != null && result.Teams.Count > 0)
                {
                    _logger.LogInformation("First team - Id: {TeamId}, Name: {TeamName}", result.Teams[0].Id, result.Teams[0].Name);
                }

                return result;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to retrieve user teams");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving user teams");
                throw;
            }
        }

        public async Task<ClickUpSpacesResponse> GetTeamSpacesAsync(string accessToken, string teamId, CancellationToken cancellationToken = default)
        {
            try
            {
                var spacesEndpoint = $"https://api.clickup.com/api/v2/team/{teamId}/space";
                var request = new HttpRequestMessage(HttpMethod.Get, spacesEndpoint);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                var response = await _httpClient.SendAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();

                var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogInformation("ClickUp spaces response: {Response}", jsonContent);

                var result = System.Text.Json.JsonSerializer.Deserialize<ClickUpSpacesResponse>(jsonContent, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                _logger.LogInformation("Retrieved team spaces. Spaces count: {SpacesCount}", result?.Spaces?.Count ?? 0);
                return result ?? throw new InvalidOperationException("Failed to deserialize spaces response");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to retrieve team spaces");
                throw;
            }
        }

        public async Task<ClickUpListsResponse> GetSpaceListsAsync(string accessToken, string spaceId, CancellationToken cancellationToken = default)
        {
            try
            {
                var listsEndpoint = $"https://api.clickup.com/api/v2/space/{spaceId}/list";
                var request = new HttpRequestMessage(HttpMethod.Get, listsEndpoint);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                var response = await _httpClient.SendAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();

                var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogInformation("ClickUp lists response: {Response}", jsonContent);

                var result = System.Text.Json.JsonSerializer.Deserialize<ClickUpListsResponse>(jsonContent, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                _logger.LogInformation("Retrieved space lists. Lists count: {ListsCount}", result?.Lists?.Count ?? 0);
                return result ?? throw new InvalidOperationException("Failed to deserialize lists response");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to retrieve space lists");
                throw;
            }
        }

        public async Task<ClickUpTasksResponse> GetSpaceTasksAsync(string accessToken, string spaceId, CancellationToken cancellationToken = default)
        {
            try
            {
                var tasksEndpoint = $"https://api.clickup.com/api/v2/space/{spaceId}/task";
                var request = new HttpRequestMessage(HttpMethod.Get, tasksEndpoint);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                var response = await _httpClient.SendAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<ClickUpTasksResponse>(cancellationToken: cancellationToken);
                _logger.LogInformation("Retrieved {TaskCount} tasks from ClickUp space {SpaceId}", result.Tasks.Count, spaceId);
                return result;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to retrieve tasks from space {SpaceId}", spaceId);
                throw;
            }
        }

        public async Task<ClickUpTasksResponse> GetListTasksAsync(string accessToken, string listId, CancellationToken cancellationToken = default)
        {
            try
            {
                var tasksEndpoint = $"https://api.clickup.com/api/v2/list/{listId}/task";
                var request = new HttpRequestMessage(HttpMethod.Get, tasksEndpoint);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                var response = await _httpClient.SendAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<ClickUpTasksResponse>(cancellationToken: cancellationToken);
                _logger.LogInformation("Retrieved {TaskCount} tasks from ClickUp list {ListId}", result.Tasks.Count, listId);
                return result;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to retrieve tasks from list {ListId}", listId);
                throw;
            }
        }

        public async Task<ClickUpTask> GetTaskAsync(string accessToken, string taskId, CancellationToken cancellationToken = default)
        {
            try
            {
                var taskEndpoint = $"https://api.clickup.com/api/v2/task/{taskId}";
                var request = new HttpRequestMessage(HttpMethod.Get, taskEndpoint);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                var response = await _httpClient.SendAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<ClickUpTask>(cancellationToken: cancellationToken);
                _logger.LogInformation("Retrieved task {TaskId} from ClickUp", taskId);
                return result;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to retrieve task {TaskId}", taskId);
                throw;
            }
        }

        public async Task<ClickUpTask> CreateTaskAsync(string accessToken, string listId, string title, string? description = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var createTaskEndpoint = $"https://api.clickup.com/api/v2/list/{listId}/task";
                var request = new HttpRequestMessage(HttpMethod.Post, createTaskEndpoint);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                var requestBody = new
                {
                    name = title,
                    description = description ?? ""
                };

                request.Content = new StringContent(
                    JsonSerializer.Serialize(requestBody),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.SendAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<ClickUpTask>(cancellationToken: cancellationToken);
                _logger.LogInformation("Created new task in ClickUp list {ListId}", listId);
                return result;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to create task in list {ListId}", listId);
                throw;
            }
        }

        public async Task<ClickUpTask> UpdateTaskAsync(string accessToken, string taskId, string? title = null, string? description = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var updateTaskEndpoint = $"https://api.clickup.com/api/v2/task/{taskId}";
                var request = new HttpRequestMessage(HttpMethod.Put, updateTaskEndpoint);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                var requestBody = new Dictionary<string, object?>();
                if (!string.IsNullOrWhiteSpace(title))
                    requestBody["name"] = title;
                if (!string.IsNullOrWhiteSpace(description))
                    requestBody["description"] = description;

                if (requestBody.Count == 0)
                    throw new ArgumentException("At least one field (title or description) must be provided for update");

                request.Content = new StringContent(
                    JsonSerializer.Serialize(requestBody),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.SendAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<ClickUpTask>(cancellationToken: cancellationToken);
                _logger.LogInformation("Updated task {TaskId}", taskId);
                return result;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to update task {TaskId}", taskId);
                throw;
            }
        }

        public async Task<bool> ValidateTokenAsync(string accessToken, CancellationToken cancellationToken = default)
        {
            try
            {
                var userEndpoint = "https://api.clickup.com/api/v2/user";
                var request = new HttpRequestMessage(HttpMethod.Get, userEndpoint);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                var response = await _httpClient.SendAsync(request, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("ClickUp token validated successfully");
                    return true;
                }

                _logger.LogWarning("ClickUp token validation failed with status {StatusCode}", response.StatusCode);
                return false;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error validating ClickUp token");
                return false;
            }
        }
    }
}
