using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Requra.Application.Interfaces.IProjectService;
using Requra.Application.Response;
using Requra.Infrastructure.ExternalDTOs.ClickUpDto;
using Requra.Infrastructure.ExternalInterfaces.IClickUpService;
using Requra.Infrastructure.Services.ClickUpService;
using System.Security.Claims;

namespace Requra.Presentation.Controllers.ClickUp
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ClickUpController : ControllerBase
    {
        private readonly IClickUpService _clickUpService;
        private readonly IClickUpSyncService _clickUpSyncService;
        private readonly IClickUpPushService _clickUpPushService;
        private readonly IProjectService _projectService;
        private readonly ILogger<ClickUpController> _logger;
        private readonly ClickUpOAuthSettings _clickUpSettings;

        public ClickUpController(
            IClickUpService clickUpService,
            IClickUpSyncService clickUpSyncService,
            IClickUpPushService clickUpPushService,
            IProjectService projectService,
            ILogger<ClickUpController> logger,
            IOptions<ClickUpOAuthSettings> clickUpSettings)
        {
            _clickUpService = clickUpService;
            _clickUpSyncService = clickUpSyncService;
            _clickUpPushService = clickUpPushService;
            _projectService = projectService;
            _logger = logger;
            _clickUpSettings = clickUpSettings.Value;
        }

        /// <summary>
        /// Initiates OAuth flow by returning the authorization URL
        /// </summary>
        [HttpGet("auth/authorize")]
        public IActionResult GetAuthorizationUrl([FromQuery] Guid projectId)
        {
            try
            {
                if (projectId == Guid.Empty)
                    return BadRequest(Response<string>.Failure("Project ID is required"));

                if (string.IsNullOrWhiteSpace(_clickUpSettings.RedirectUri))
                    return StatusCode(500, Response<string>.Failure("ClickUp redirect URI not configured in settings"));

                var authUrl = _clickUpService.GetAuthorizationUrl(_clickUpSettings.RedirectUri);

                return Ok(Response<object>.Success(new
                {
                    authUrl,
                    projectId
                }, "Authorization URL generated"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating authorization URL");
                return StatusCode(500, Response<string>.Failure("Error generating authorization URL"));
            }
        }

        /// <summary>
        /// Callback endpoint for OAuth authorization code exchange
        /// </summary>
        [HttpPost("auth/callback")]
        [AllowAnonymous]
        public async Task<IActionResult> OAuthCallback([FromBody] OAuthCallbackRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Code))
                    return BadRequest(Response<string>.Failure("Authorization code is required"));

                if (request.ProjectId == Guid.Empty)
                    return BadRequest(Response<string>.Failure("Project ID is required"));

                // Exchange authorization code for access token
                var tokenResponse = await _clickUpService.ExchangeAuthorizationCodeAsync(request.Code);

                // Get authorized user info
                var userInfo = await _clickUpService.GetAuthorizedUserAsync(tokenResponse.AccessToken);

                // Get user teams/workspaces
                var teamsResponse = await _clickUpService.GetUserTeamsAsync(tokenResponse.AccessToken);

                // Connect project to ClickUp
                var team = teamsResponse.Teams.FirstOrDefault();
                if (team == null)
                    return BadRequest(Response<string>.Failure("No ClickUp team found"));

                var teamId = team.Id;

                // Get spaces for this team
                var spacesResponse = await _clickUpService.GetTeamSpacesAsync(tokenResponse.AccessToken, teamId);
                var space = spacesResponse.Spaces.FirstOrDefault();

                if (space == null)
                    return BadRequest(Response<string>.Failure("No ClickUp space found in team"));

                var spaceId = space.Id;

                // Get lists from the space
                var listsResponse = await _clickUpService.GetSpaceListsAsync(tokenResponse.AccessToken, spaceId);
                var listId = listsResponse.Lists.FirstOrDefault()?.Id;

                _logger.LogInformation("OAuth callback - TeamId: {TeamId}, SpaceId: {SpaceId}, ListId: {ListId}, Token: {Token}",
                    teamId, spaceId, listId, !string.IsNullOrEmpty(tokenResponse.AccessToken));

                if (string.IsNullOrWhiteSpace(listId))
                    return BadRequest(Response<string>.Failure("No ClickUp lists found in space"));

                await _projectService.ConnectClickUpAsync(
                    request.ProjectId,
                    tokenResponse.AccessToken,
                    teamId,
                    spaceId,
                    listId,
                    tokenResponse.ExpiresIn
                );

                return Ok(Response<object>.Success(new
                {
                    teamId,
                    spaceId,
                    listId,
                    username = userInfo.User.Username
                }, "Successfully connected to ClickUp"));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid configuration in OAuth callback");
                return BadRequest(Response<string>.Failure($"Configuration error: {ex.Message}"));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error in OAuth callback");
                return BadRequest(Response<string>.Failure($"ClickUp API error: {ex.Message}"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OAuth callback");
                return StatusCode(500, Response<string>.Failure($"Error processing OAuth callback: {ex.Message}"));
            }
        }

        /// <summary>
        /// Syncs all ClickUp tasks for a project to UserStories
        /// </summary>
        //[HttpPost("sync/{projectId}")]
        //public async Task<IActionResult> SyncProjectTasks(Guid projectId)
        //{
        //    try
        //    {
        //        //var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //        //if (string.IsNullOrWhiteSpace(userId))
        //        //    return Unauthorized();

        //        //// Verify user has access to project
        //        //var hasAccess = await _projectService.UserHasAccessToProjectAsync(projectId, userId);
        //        //if (!hasAccess)
        //        //    return Forbid();

        //        var syncedCount = await _clickUpSyncService.SyncProjectTasksAsync(projectId);

        //        return Ok(Response<object>.Success(new
        //        {
        //            syncedCount
        //        }, $"Successfully synced {syncedCount} tasks"));
        //    }
        //    catch (InvalidOperationException ex)
        //    {
        //        _logger.LogWarning(ex, "Project not connected to ClickUp");
        //        return BadRequest(Response<string>.Failure(ex.Message));
        //    }
        //    catch (HttpRequestException ex)
        //    {
        //        _logger.LogError(ex, "HTTP error syncing ClickUp tasks for project {ProjectId}", projectId);
        //        return StatusCode(500, Response<string>.Failure($"ClickUp API error: {ex.Message}"));
        //    }
        //    catch (Exception ex)
        //    {
        //        var message = ex.InnerException?.Message ?? ex.Message;
        //        _logger.LogError(ex, "Error syncing ClickUp tasks for project {ProjectId}. InnerException: {InnerMessage}", projectId, ex.InnerException?.Message);
        //        return StatusCode(500, Response<string>.Failure($"Error syncing ClickUp tasks: {message}"));
        //    }
        //}

        /// <summary>
        /// Syncs ClickUp tasks from a specific list
        /// </summary>
        //[HttpPost("sync/{projectId}/list/{listId}")]
        //public async Task<IActionResult> SyncListTasks(Guid projectId, string listId)
        //{
        //    try
        //    {
        //        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //        if (string.IsNullOrWhiteSpace(userId))
        //            return Unauthorized();

        //        // Verify user has access to project
        //        var hasAccess = await _projectService.UserHasAccessToProjectAsync(projectId, userId);
        //        if (!hasAccess)
        //            return Forbid();

        //        if (string.IsNullOrWhiteSpace(listId))
        //            return BadRequest(Response<string>.Failure("List ID is required"));

        //        var syncedCount = await _clickUpSyncService.SyncListTasksAsync(projectId, listId);

        //        return Ok(Response<object>.Success(new
        //        {
        //            syncedCount,
        //            listId
        //        }, $"Successfully synced {syncedCount} tasks from list"));
        //    }
        //    catch (InvalidOperationException ex)
        //    {
        //        _logger.LogWarning(ex, "Project not connected to ClickUp");
        //        return BadRequest(Response<string>.Failure(ex.Message));
        //    }
        //    catch (HttpRequestException ex)
        //    {
        //        _logger.LogError(ex, "HTTP error syncing list tasks");
        //        return StatusCode(500, Response<string>.Failure($"ClickUp API error: {ex.Message}"));
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error syncing list tasks");
        //        return StatusCode(500, Response<string>.Failure($"Error syncing list tasks: {ex.Message}"));
        //    }
        //}

        /// <summary>
        /// Disconnects a project from ClickUp
        /// </summary>
        [HttpPost("disconnect/{projectId}")]
        public async Task<IActionResult> DisconnectClickUp(Guid projectId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrWhiteSpace(userId))
                    return Unauthorized();

                // Verify user has access to project
                var hasAccess = await _projectService.UserHasAccessToProjectAsync(projectId, userId);
                if (!hasAccess)
                    return Forbid();

                await _projectService.DisconnectClickUpAsync(projectId);

                return Ok(Response<string>.Success(null, "Successfully disconnected from ClickUp"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disconnecting from ClickUp");
                return StatusCode(500, Response<string>.Failure("Error disconnecting from ClickUp"));
            }
        }

        /// <summary>
        /// Gets ClickUp connection status for a project
        /// </summary>
        [HttpGet("status/{projectId}")]
        public async Task<IActionResult> GetConnectionStatus(Guid projectId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrWhiteSpace(userId))
                    return Unauthorized();

                // Verify user has access to project
                var hasAccess = await _projectService.UserHasAccessToProjectAsync(projectId, userId);
                if (!hasAccess)
                    return Forbid();

                var status = await _projectService.GetClickUpConnectionStatusAsync(projectId);

                return Ok(Response<object>.Success(status, "Connection status retrieved"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving connection status");
                return StatusCode(500, Response<string>.Failure("Error retrieving connection status"));
            }
        }

        /// <summary>
        /// Pushes all UserStories from a project to ClickUp
        /// Creates new tasks for UserStories without ClickUp IDs, updates existing ones
        /// </summary>
        //[HttpPost("push/{projectId}")]
        //public async Task<IActionResult> PushProjectTasks(Guid projectId)
        //{
        //    try
        //    {
        //        //var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //        //if (string.IsNullOrWhiteSpace(userId))
        //        //    return Unauthorized();

        //        //// Verify user has access to project
        //        //var hasAccess = await _projectService.UserHasAccessToProjectAsync(projectId, userId);
        //        //if (!hasAccess)
        //        //    return Forbid();

        //        var result = await _clickUpPushService.PushProjectTasksAsync(projectId);

        //        return Ok(Response<PushProjectTasksResult>.Success(result, result.Message ?? "UserStories pushed successfully"));
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error pushing UserStories to ClickUp for project {ProjectId}", projectId);
        //        return StatusCode(500, Response<string>.Failure("Error pushing UserStories to ClickUp"));
        //    }
        //}

        /// <summary>
        /// Pushes only approved UserStories from a project to ClickUp
        /// </summary>
        [HttpPost("push/{projectId}/approved")]
        public async Task<IActionResult> PushApprovedTasks(Guid projectId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrWhiteSpace(userId))
                    return Unauthorized();

                // Verify user has access to project
                var hasAccess = await _projectService.UserHasAccessToProjectAsync(projectId, userId);
                if (!hasAccess)
                    return Forbid();

                var result = await _clickUpPushService.PushApprovedTasksAsync(projectId);

                return Ok(Response<PushProjectTasksResult>.Success(result, result.Message ?? "Approved UserStories pushed successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error pushing approved UserStories to ClickUp for project {ProjectId}", projectId);
                return StatusCode(500, Response<string>.Failure("Error pushing approved UserStories to ClickUp"));
            }
        }

        /// <summary>
        /// Pushes a single UserStory to ClickUp
        /// </summary>
        //[HttpPost("push/{projectId}/story/{userStoryId}")]
        //public async Task<IActionResult> PushSingleTask(Guid projectId, Guid userStoryId)
        //{
        //    try
        //    {
        //        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //        if (string.IsNullOrWhiteSpace(userId))
        //            return Unauthorized();

        //        // Verify user has access to project
        //        var hasAccess = await _projectService.UserHasAccessToProjectAsync(projectId, userId);
        //        if (!hasAccess)
        //            return Forbid();

        //        var result = await _clickUpPushService.PushTaskAsync(projectId, userStoryId);

        //        if (!result.Success)
        //            return BadRequest(Response<PushTaskResult>.Failure(result.Message));

        //        return Ok(Response<PushTaskResult>.Success(result, result.Message ?? "UserStory pushed successfully"));
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error pushing UserStory {UserStoryId} to ClickUp", userStoryId);
        //        return StatusCode(500, Response<string>.Failure("Error pushing UserStory to ClickUp"));
        //    }
        //}
    }

    public class OAuthCallbackRequest
    {
        public string Code { get; set; } = null!;
        public Guid ProjectId { get; set; }
    }
}
