using Requra.Infrastructure.ExternalDTOs.ClickUpDto;

namespace Requra.Infrastructure.ExternalInterfaces.IClickUpService
{
    public interface IClickUpService
    {
        /// <summary>
        /// Generates the OAuth authorization URL for user to grant access
        /// </summary>
        string GetAuthorizationUrl(string redirectUri);

        /// <summary>
        /// Exchanges authorization code for access token
        /// </summary>
        Task<ClickUpTokenResponse> ExchangeAuthorizationCodeAsync(string code, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the authorized user's workspace information and teams
        /// </summary>
        Task<ClickUpWorkspaceResponse> GetAuthorizedUserAsync(string accessToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the user's teams/workspaces
        /// </summary>
        Task<ClickUpTeamsResponse> GetUserTeamsAsync(string accessToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets spaces for a specific team
        /// </summary>
        Task<ClickUpSpacesResponse> GetTeamSpacesAsync(string accessToken, string teamId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets lists from a specific ClickUp space
        /// </summary>
        Task<ClickUpListsResponse> GetSpaceListsAsync(string accessToken, string spaceId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets tasks from a specific ClickUp space
        /// </summary>
        Task<ClickUpTasksResponse> GetSpaceTasksAsync(string accessToken, string spaceId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets tasks from a specific ClickUp list
        /// </summary>
        Task<ClickUpTasksResponse> GetListTasksAsync(string accessToken, string listId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a single task by ID
        /// </summary>
        Task<ClickUpTask> GetTaskAsync(string accessToken, string taskId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new task in ClickUp
        /// </summary>
        Task<ClickUpTask> CreateTaskAsync(string accessToken, string listId, string title, string? description = null, int? priority = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing task in ClickUp
        /// </summary>
        Task<ClickUpTask> UpdateTaskAsync(string accessToken, string taskId, string? title = null, string? description = null, int? priority = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates if the token is still valid
        /// </summary>
        Task<bool> ValidateTokenAsync(string accessToken, CancellationToken cancellationToken = default);
    }
}
