using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Requra.Application.DTOs.AI;
using Requra.Application.Interfaces.IProjectService;
using Requra.Application.Response;
using Requra.Domain.Entities;
using Requra.Domain.Enums;
using Requra.Infrastructure.Data;
using Requra.Infrastructure.ExternalDTOs.ClickUpDto;
using Requra.Infrastructure.ExternalInterfaces.IClickUpService;
using Requra.Infrastructure.Services.ClickUpService;
using System.Security.Claims;
using System.Text.Json;

namespace Requra.Presentation.Controllers.ClickUp
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [EnableRateLimiting("Integration")]
    public class ClickUpController : ControllerBase
    {
        private readonly IClickUpService _clickUpService;
        private readonly IClickUpSyncService _clickUpSyncService;
        private readonly IClickUpPushService _clickUpPushService;
        private readonly IProjectService _projectService;
        private readonly ILogger<ClickUpController> _logger;
        private readonly ClickUpOAuthSettings _clickUpSettings;
        private readonly RequraDbContext _context;


        public ClickUpController(
            IClickUpService clickUpService,
            IClickUpSyncService clickUpSyncService,
            IClickUpPushService clickUpPushService,
            IProjectService projectService,
            ILogger<ClickUpController> logger,
            IOptions<ClickUpOAuthSettings> clickUpSettings,
            RequraDbContext dbContext
            )
        {
            _clickUpService = clickUpService;
            _clickUpSyncService = clickUpSyncService;
            _clickUpPushService = clickUpPushService;
            _projectService = projectService;
            _logger = logger;
            _clickUpSettings = clickUpSettings.Value;
            _context = dbContext;
        }

        /// <summary>
        /// Initiates OAuth flow by returning the authorization URL
        /// </summary>
        [HttpGet("auth/authorize")]
        public IActionResult GetAuthorizationUrl([FromQuery] Guid projectId, [FromQuery] ClientPlatform platform = ClientPlatform.Web)
        {
            try
            {
                if (projectId == Guid.Empty)
                    return BadRequest(Response<string>.Failure("Project ID is required"));

                var redirectUri = GetPlatformSpecificCallbackUrl(platform);
                var authUrl = _clickUpService.GetAuthorizationUrl(redirectUri);

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
        //        [HttpPost("test-push/{projectId}")]
        //public async Task<IActionResult> TestPushApprovedTasks(
        //    Guid projectId,
        //    [FromBody] JsonElement rawJson,
        //    CancellationToken cancellationToken)
        //{
        //    try
        //    {
        //        // Convert request body back to raw JSON string
        //        var rawJsonString = rawJson.GetRawText();

        //        if (string.IsNullOrWhiteSpace(rawJsonString))
        //        {
        //            return BadRequest(
        //                Response<string>.Failure(
        //                    "Raw JSON body is required."));
        //        }

        //        // 1. Deserialize the raw JSON
        //        var aiResult = JsonSerializer.Deserialize<JobResultResponseDto>(
        //            rawJsonString,
        //            new JsonSerializerOptions
        //            {
        //                PropertyNameCaseInsensitive = true
        //            });

        //        if (aiResult?.UserStories == null ||
        //            aiResult.UserStories.Count == 0)
        //        {
        //            return BadRequest(
        //                Response<string>.Failure(
        //                    "Raw JSON does not contain any user stories."));
        //        }

        //        var mappedCount = 0;
        //        var skippedCount = 0;

        //        // 2. Map User Stories
        //        foreach (var aiUserStory in aiResult.UserStories)
        //        {
        //                    // Validate ID
        //                    if (string.IsNullOrWhiteSpace(aiUserStory.Id))
        //                    {
        //                        skippedCount++;
        //                        continue;
        //                    }

        //                    // Validate Title
        //                    if (string.IsNullOrWhiteSpace(aiUserStory.Title))
        //                    {
        //                        skippedCount++;
        //                        continue;
        //                    }

        //                    // 3. Check if User Story already exists
        //                    var alreadyExists = await _context.UserStories
        //                .AnyAsync(
        //                    x => x.ProjectId == projectId &&
        //                         x.SourceUserStoryId == aiUserStory.Id,
        //                    cancellationToken);

        //                    if (alreadyExists)
        //                    {
        //                        skippedCount++;
        //                        continue;
        //                    }

        //                    // 4. Find Requirement
        //                //    var requirement = await _context.Requirements
        //                //.FirstOrDefaultAsync(
        //                //    x => x.ProjectId == projectId &&
        //                //         x.SourceRequirementId == aiUserStory.RequirementId,
        //                //    cancellationToken);

        //            //if (requirement == null)
        //            //{
        //            //    skippedCount++;
        //            //    continue;
        //            //}

        //            // 5. Map Priority
        //            var priority = aiUserStory.Priority?
        //                .Trim()
        //                .ToLowerInvariant() switch
        //            {
        //                "low" => UserStoryPriority.low,
        //                "medium" => UserStoryPriority.medium,
        //                "high" => UserStoryPriority.high,
        //                "critical" => UserStoryPriority.critical,
        //                _ => UserStoryPriority.medium
        //            };

        //            // 6. Map Type
        //            var type = aiUserStory.Type?
        //                .Trim()
        //                .ToLowerInvariant() switch
        //            {
        //                "functional" => UserStoryType.Functional,
        //                "non-functional" => UserStoryType.NonFunctional,

        //                _ => throw new ArgumentException(
        //                    $"Unknown user story type: {aiUserStory.Type}")
        //            };

        //            // 7. New User Story status
        //            var status = UserStoryStatus.NeedReview;

        //            // 8. Map Acceptance Criteria
        //            var acceptanceCriteria = new List<AcceptanceCriterion>();

        //            foreach (var aiCriterion in
        //                     aiUserStory.AcceptanceCriteria ??
        //                     Enumerable.Empty<AcceptanceCriteriaDto>())
        //            {
        //                if (string.IsNullOrWhiteSpace(aiCriterion.Text))
        //                    continue;

        //                var criterion = new AcceptanceCriterion(
        //                    sourceAcceptanceCriterionId: aiCriterion.Id,
        //                    text: aiCriterion.Text,
        //                    criterionType: aiCriterion.CriterionType
        //                );

        //                acceptanceCriteria.Add(criterion);
        //            }

        //            // 9. Create User Story
        //            var userStory = new UserStory(
        //    sourceUserStoryId: aiUserStory.Id,
        //    title: aiUserStory.Title,
        //    description: aiUserStory.UserStory,
        //    acceptanceCriteria: acceptanceCriteria,
        //    type: type,
        //    status: status,
        //    priority: priority,
        //    language: Language.En,
        //    creatorId: "01f535f1-9870-4141-9b29-21df2d9cd6ec",
        //    requirementId: Guid.Parse("ee5d2f32-27df-48d7-9ac4-784d6678ce9a"),
        //    projectId: projectId,
        //    storyPoints: aiUserStory.JiraFields?.StoryPoints,
        //    sourceRequirementId: aiUserStory.RequirementId,
        //    deduplicationKey: aiUserStory.DeduplicationKey
        //);

        //            // 10. Map Source References
        //            foreach (var sourceReference in
        //                     aiUserStory.SourceRefs ??
        //                     Enumerable.Empty<UserStorySourceRefDto>())
        //            {
        //                var reference = new UserStorySourceRef(
        //                    page: sourceReference.Page,
        //                    quote: sourceReference.Quote,
        //                    chunkId: sourceReference.ChunkId,
        //                    sourceId: sourceReference.SourceId,
        //                    sourceType: sourceReference.SourceType,
        //                    documentName: sourceReference.DocumentName,
        //                    confidenceScore: sourceReference.ConfidenceScore
        //                );

        //                userStory.AddSourceReference(reference);
        //            }

        //            // 11. Add User Story
        //            _context.UserStories.Add(userStory);

        //            mappedCount++;
        //        }

        //        // 12. Save
        //        await _context.SaveChangesAsync(cancellationToken);

        //        return Ok(
        //            Response<object>.Success(
        //                new
        //                {
        //                    ProjectId = projectId,
        //                    TotalAiUserStories = aiResult.UserStories.Count,
        //                    MappedUserStories = mappedCount,
        //                    SkippedUserStories = skippedCount
        //                },
        //                "Test mapping completed successfully"
        //            ));
        //    }
        //    catch (JsonException ex)
        //    {
        //        _logger.LogError(
        //            ex,
        //            "Invalid Raw JSON while testing UserStory mapping for project {ProjectId}",
        //            projectId);

        //        return BadRequest(
        //            Response<string>.Failure(
        //                $"Invalid JSON: {ex.Message}"));
        //    }
        //    catch (ArgumentException ex)
        //    {
        //        _logger.LogError(
        //            ex,
        //            "Invalid AI value while mapping UserStories for project {ProjectId}",
        //            projectId);

        //        return BadRequest(
        //            Response<string>.Failure(ex.Message));
        //    }
        //            catch (DbUpdateException ex)
        //            {
        //                var innerMessage = ex.InnerException?.Message;

        //                _logger.LogError(
        //                    ex,
        //                    "Database error while mapping UserStories for project {ProjectId}. Inner: {InnerMessage}",
        //                    projectId,
        //                    innerMessage);

        //                return StatusCode(
        //                    500,
        //                    Response<string>.Failure(
        //                        $"Database error: {innerMessage ?? ex.Message}"));
        //            }
        //        }

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

     //   [HttpPost("test-map-requirements/{projectId}")]
     //   public async Task<IActionResult> TestMapRequirements(
     //Guid projectId,
     //[FromBody] JsonElement rawJson,
     //CancellationToken cancellationToken)
     //   {
     //       try
     //       {
     //           // Convert request body to raw JSON
     //           var rawJsonString = rawJson.GetRawText();

     //           if (string.IsNullOrWhiteSpace(rawJsonString))
     //           {
     //               return BadRequest(
     //                   Response<string>.Failure(
     //                       "Raw JSON body is required."));
     //           }

     //           // 1. Deserialize complete AI result
     //           var aiResult = JsonSerializer.Deserialize<JobResultResponseDto>(
     //               rawJsonString,
     //               new JsonSerializerOptions
     //               {
     //                   PropertyNameCaseInsensitive = true
     //               });

     //           if (aiResult?.Requirements == null ||
     //               aiResult.Requirements.Count == 0)
     //           {
     //               return BadRequest(
     //                   Response<string>.Failure(
     //                       "Raw JSON does not contain any requirements."));
     //           }

     //           var mappedCount = 0;
     //           var skippedCount = 0;

     //           // 2. Map Requirements
     //           foreach (var aiRequirement in aiResult.Requirements)
     //           {
     //               // -----------------------------------------
     //               // Validate ID
     //               // -----------------------------------------
     //               if (string.IsNullOrWhiteSpace(aiRequirement.Id))
     //               {
     //                   skippedCount++;
     //                   continue;
     //               }

     //               // -----------------------------------------
     //               // Validate Title
     //               // -----------------------------------------
     //               if (string.IsNullOrWhiteSpace(aiRequirement.Title))
     //               {
     //                   skippedCount++;
     //                   continue;
     //               }

     //               // -----------------------------------------
     //               // Check if Requirement already exists
     //               // -----------------------------------------
     //               var alreadyExists = await _context.Requirements
     //                   .AnyAsync(
     //                       x => x.ProjectId == projectId &&
     //                            x.SourceRequirementId == aiRequirement.Id,
     //                       cancellationToken);

     //               if (alreadyExists)
     //               {
     //                   skippedCount++;
     //                   continue;
     //               }

     //               // -----------------------------------------
     //               // Map Requirement Type
     //               // -----------------------------------------
     //               var requirementType =
     //                   aiRequirement.Type?
     //                       .Trim()
     //                       .ToLowerInvariant() switch
     //                   {
     //                       "functional" =>
     //                           RequirementType.Functional,

     //                       "non-functional" =>
     //                           RequirementType.Non_Functional,

     //                       "business" =>
     //                           RequirementType.Business_Rule,

     //                       _ => throw new ArgumentException(
     //                           $"Unknown requirement type: {aiRequirement.Type}")
     //                   };

     //               // -----------------------------------------
     //               // Serialize Quality Issues
     //               //
     //               // These come from:
     //               //
     //               // requirement.quality.issues
     //               //
     //               // NOT root:
     //               //
     //               // quality_issues
     //               // -----------------------------------------
     //               var qualityIssues =
     //                   aiRequirement.Quality?.Issues == null ||
     //                   aiRequirement.Quality.Issues.Count == 0
     //                       ? null
     //                       : JsonSerializer.Serialize(
     //                           aiRequirement.Quality.Issues);

     //               // -----------------------------------------
     //               // Serialize Quality Warnings
     //               // -----------------------------------------
     //               var qualityWarnings =
     //                   aiRequirement.Quality?.Warnings == null ||
     //                   aiRequirement.Quality.Warnings.Count == 0
     //                       ? null
     //                       : JsonSerializer.Serialize(
     //                           aiRequirement.Quality.Warnings);

     //               // -----------------------------------------
     //               // Create Requirement
     //               // -----------------------------------------
     //               var requirement = new Requirement(
     //                   sourceRequirementId: aiRequirement.Id,
     //                   title: aiRequirement.Title,
     //                   description: aiRequirement.Description,
     //                   type: requirementType,
     //                   projectId: projectId,

     //                   confidenceScore:
     //                       aiRequirement.ConfidenceScore,

     //                   qualityScore:
     //                       aiRequirement.Quality?.Score,

     //                   qualityIssues:
     //                       qualityIssues,

     //                   qualityWarnings:
     //                       qualityWarnings,

     //                   deduplicationKey:
     //                       aiRequirement.DeduplicationKey,

     //                   actor:
     //                       aiRequirement.Actor,

     //                   category:
     //                       aiRequirement.Category,

     //                   priority:
     //                       aiRequirement.Priority
     //               );

     //               // -----------------------------------------
     //               // Map Source References
     //               // -----------------------------------------
     //               foreach (var sourceReference in
     //                        aiRequirement.SourceRefs ??
     //                        Enumerable.Empty<RequirementSourceRefDto>())
     //               {
     //                   var reference = new RequirementSourceReference(
     //                       page: sourceReference.Page,
     //                       quote: sourceReference.Quote,
     //                       chunkId: sourceReference.ChunkId,
     //                       sourceId: sourceReference.SourceId,
     //                       sourceType: sourceReference.SourceType,
     //                       documentName: sourceReference.DocumentName,
     //                       confidenceScore: sourceReference.ConfidenceScore
     //                   );

     //                   requirement.AddSourceReference(reference);
     //               }

     //               // -----------------------------------------
     //               // Add Requirement
     //               // -----------------------------------------
     //               _context.Requirements.Add(requirement);

     //               mappedCount++;
     //           }

     //           // -----------------------------------------
     //           // Save changes
     //           // -----------------------------------------
     //           await _context.SaveChangesAsync(cancellationToken);

     //           // -----------------------------------------
     //           // Return result
     //           // -----------------------------------------
     //           return Ok(
     //               Response<object>.Success(
     //                   new
     //                   {
     //                       ProjectId = projectId,
     //                       TotalAiRequirements = aiResult.Requirements.Count,
     //                       MappedRequirements = mappedCount,
     //                       SkippedRequirements = skippedCount
     //                   },
     //                   "Test requirement mapping completed successfully"
     //               ));
     //       }
     //       catch (JsonException ex)
     //       {
     //           _logger.LogError(
     //               ex,
     //               "Invalid Raw JSON while testing Requirement mapping for project {ProjectId}",
     //               projectId);

     //           return BadRequest(
     //               Response<string>.Failure(
     //                   $"Invalid JSON: {ex.Message}"));
     //       }
     //       catch (ArgumentException ex)
     //       {
     //           _logger.LogError(
     //               ex,
     //               "Invalid AI value while mapping Requirements for project {ProjectId}",
     //               projectId);

     //           return BadRequest(
     //               Response<string>.Failure(
     //                   ex.Message));
     //       }
     //       catch (DbUpdateException ex)
     //       {
     //           var innerMessage = ex.InnerException?.Message;

     //           _logger.LogError(
     //               ex,
     //               "Database error while mapping Requirements for project {ProjectId}. Inner: {InnerMessage}",
     //               projectId,
     //               innerMessage);

     //           return StatusCode(
     //               500,
     //               Response<string>.Failure(
     //                   $"Database error: {innerMessage ?? ex.Message}"));
     //       }
     //       catch (Exception ex)
     //       {
     //           _logger.LogError(
     //               ex,
     //               "Error during test mapping of Requirements for project {ProjectId}",
     //               projectId);

     //           return StatusCode(
     //               500,
     //               Response<string>.Failure(
     //                   "Error during test mapping of Requirements"));
     //       }
     //   }

        /// <summary>
        /// Generates the platform-specific callback URL for ClickUp OAuth
        /// </summary>
        private string GetPlatformSpecificCallbackUrl(ClientPlatform platform)
        {
            return platform switch
            {
                ClientPlatform.Web => "https://requra-ai.vercel.app/integrations/clickup/callback",
                ClientPlatform.Mobile => "requra://clickup/callback",
                _ => "https://requra-ai.vercel.app/integrations/clickup/callback"
            };
        }
    }

    public class OAuthCallbackRequest
    {
        public string Code { get; set; } = null!;
        public Guid ProjectId { get; set; }
    }
}
