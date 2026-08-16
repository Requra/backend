using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Requra.Domain.Entities;
using Requra.Domain.Enums;
using Requra.Infrastructure.Data;
using Requra.Infrastructure.ExternalInterfaces.IClickUpService;

namespace Requra.Infrastructure.Services.ClickUpService
{
    public class ClickUpPushService : IClickUpPushService
    {
        private readonly RequraDbContext _dbContext;
        private readonly IClickUpService _clickUpService;
        private readonly ILogger<ClickUpPushService> _logger;

        public ClickUpPushService(
            RequraDbContext dbContext,
            IClickUpService clickUpService,
            ILogger<ClickUpPushService> logger)
        {
            _dbContext = dbContext;
            _clickUpService = clickUpService;
            _logger = logger;
        }

        public async Task<PushProjectTasksResult> PushProjectTasksAsync(Guid projectId, CancellationToken cancellationToken = default)
        {
            var result = new PushProjectTasksResult { ProjectId = projectId };

            try
            {
                var project = await _dbContext.Projects
                    .FirstOrDefaultAsync(p => p.Id == projectId && p.IsClickUpConnected, cancellationToken);

                if (project == null)
                {
                    _logger.LogWarning("Project {ProjectId} not found or not connected to ClickUp", projectId);
                    result.Message = "Project not found or not connected to ClickUp";
                    return result;
                }

                if (project.IsClickUpTokenExpired())
                {
                    _logger.LogWarning("ClickUp token for project {ProjectId} has expired", projectId);
                    project.DisconnectFromClickUp();
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    result.Message = "ClickUp token has expired";
                    return result;
                }

                if (string.IsNullOrWhiteSpace(project.ClickUpAccessToken) || string.IsNullOrWhiteSpace(project.ClickUpListId))
                {
                    _logger.LogWarning("Project {ProjectId} is missing ClickUp access token or list ID", projectId);
                    result.Message = "Project is missing ClickUp access token or list ID";
                    return result;
                }

                // Get all UserStories for this project
                var userStories = await _dbContext.UserStories
                    .Where(u => u.ProjectId == projectId)
                    .ToListAsync(cancellationToken);

                if (!userStories.Any())
                {
                    _logger.LogInformation("No UserStories found for project {ProjectId}", projectId);
                    result.Message = "No UserStories to push";
                    return result;
                }

                _logger.LogInformation("Pushing {Count} UserStories to ClickUp for project {ProjectId}", 
                    userStories.Count, projectId);

                // Push each UserStory
                foreach (var userStory in userStories)
                {
                    try
                    {
                        var pushResult = await PushUserStoryToClickUpAsync(
                            userStory,
                            project.ClickUpAccessToken,
                            project.ClickUpListId,
                            cancellationToken);

                        result.Details.Add(pushResult);

                        switch (pushResult.Action)
                        {
                            case PushAction.Created:
                                result.CreatedCount++;
                                break;
                            case PushAction.Updated:
                                result.UpdatedCount++;
                                break;
                            case PushAction.Skipped:
                                result.SkippedCount++;
                                break;
                            case PushAction.Failed:
                                result.FailedCount++;
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to push UserStory {UserStoryId} to ClickUp", userStory.Id);
                        result.Details.Add(new PushTaskResult
                        {
                            UserStoryId = userStory.Id,
                            Action = PushAction.Failed,
                            Success = false,
                            Message = ex.Message
                        });
                        result.FailedCount++;
                    }
                }

                await _dbContext.SaveChangesAsync(cancellationToken);

                result.Message = $"Successfully pushed {result.CreatedCount} new tasks and updated {result.UpdatedCount} existing tasks";
                _logger.LogInformation("Completed pushing UserStories for project {ProjectId}: Created={Created}, Updated={Updated}, Failed={Failed}, Skipped={Skipped}",
                    projectId, result.CreatedCount, result.UpdatedCount, result.FailedCount, result.SkippedCount);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error pushing project tasks to ClickUp for project {ProjectId}", projectId);
                result.Message = $"Error: {ex.Message}";
                return result;
            }
        }

        public async Task<PushProjectTasksResult> PushApprovedTasksAsync(Guid projectId, CancellationToken cancellationToken = default)
        {
            var result = new PushProjectTasksResult { ProjectId = projectId };

            try
            {
                var project = await _dbContext.Projects
                    .FirstOrDefaultAsync(p => p.Id == projectId && p.IsClickUpConnected, cancellationToken);

                if (project == null)
                {
                    _logger.LogWarning("Project {ProjectId} not found or not connected to ClickUp", projectId);
                    result.Message = "Project not found or not connected to ClickUp";
                    return result;
                }

                if (project.IsClickUpTokenExpired())
                {
                    _logger.LogWarning("ClickUp token for project {ProjectId} has expired", projectId);
                    project.DisconnectFromClickUp();
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    result.Message = "ClickUp token has expired";
                    return result;
                }

                if (string.IsNullOrWhiteSpace(project.ClickUpAccessToken) || string.IsNullOrWhiteSpace(project.ClickUpListId))
                {
                    _logger.LogWarning("Project {ProjectId} is missing ClickUp access token or list ID", projectId);
                    result.Message = "Project is missing ClickUp access token or list ID";
                    return result;
                }

                // Get only approved UserStories for this project
                var userStories = await _dbContext.UserStories
                    .Where(u => u.ProjectId == projectId && 
                                u.Status == UserStoryStatus.Approved)
                    .ToListAsync(cancellationToken);

                if (!userStories.Any())
                {
                    _logger.LogInformation("No approved UserStories found for project {ProjectId}", projectId);
                    result.Message = "No approved UserStories to push";
                    return result;
                }

                _logger.LogInformation("Pushing {Count} approved UserStories to ClickUp for project {ProjectId}",
                    userStories.Count, projectId);

                // Push each UserStory
                foreach (var userStory in userStories)
                {
                    try
                    {
                        var pushResult = await PushUserStoryToClickUpAsync(
                            userStory,
                            project.ClickUpAccessToken,
                            project.ClickUpListId,
                            cancellationToken);

                        result.Details.Add(pushResult);

                        switch (pushResult.Action)
                        {
                            case PushAction.Created:
                                result.CreatedCount++;
                                break;
                            case PushAction.Updated:
                                result.UpdatedCount++;
                                break;
                            case PushAction.Skipped:
                                result.SkippedCount++;
                                break;
                            case PushAction.Failed:
                                result.FailedCount++;
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to push UserStory {UserStoryId} to ClickUp", userStory.Id);
                        result.Details.Add(new PushTaskResult
                        {
                            UserStoryId = userStory.Id,
                            Action = PushAction.Failed,
                            Success = false,
                            Message = ex.Message
                        });
                        result.FailedCount++;
                    }
                }

                await _dbContext.SaveChangesAsync(cancellationToken);

                result.Message = $"Successfully pushed {result.CreatedCount} new approved tasks and updated {result.UpdatedCount} existing tasks";
                _logger.LogInformation("Completed pushing approved UserStories for project {ProjectId}: Created={Created}, Updated={Updated}, Failed={Failed}",
                    projectId, result.CreatedCount, result.UpdatedCount, result.FailedCount);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error pushing approved tasks to ClickUp for project {ProjectId}", projectId);
                result.Message = $"Error: {ex.Message}";
                return result;
            }
        }

        public async Task<PushTaskResult> PushTaskAsync(Guid projectId, Guid userStoryId, CancellationToken cancellationToken = default)
        {
            var result = new PushTaskResult { UserStoryId = userStoryId };

            try
            {
                var project = await _dbContext.Projects
                    .FirstOrDefaultAsync(p => p.Id == projectId && p.IsClickUpConnected, cancellationToken);

                if (project == null)
                {
                    result.Success = false;
                    result.Action = PushAction.Failed;
                    result.Message = "Project not found or not connected to ClickUp";
                    return result;
                }

                if (string.IsNullOrWhiteSpace(project.ClickUpAccessToken) || string.IsNullOrWhiteSpace(project.ClickUpListId))
                {
                    result.Success = false;
                    result.Action = PushAction.Failed;
                    result.Message = "Project is missing ClickUp access token or list ID";
                    return result;
                }

                var userStory = await _dbContext.UserStories
                    .FirstOrDefaultAsync(u => u.Id == userStoryId && u.ProjectId == projectId, cancellationToken);

                if (userStory == null)
                {
                    result.Success = false;
                    result.Action = PushAction.Failed;
                    result.Message = "UserStory not found";
                    return result;
                }

                return await PushUserStoryToClickUpAsync(userStory, project.ClickUpAccessToken, project.ClickUpListId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error pushing UserStory {UserStoryId} to ClickUp", userStoryId);
                result.Success = false;
                result.Action = PushAction.Failed;
                result.Message = ex.Message;
                return result;
            }
        }

        /// <summary>
        /// Internal method to push a single UserStory to ClickUp
        /// </summary>
        private async Task<PushTaskResult> PushUserStoryToClickUpAsync(
            UserStory userStory,
            string accessToken,
            string listId,
            CancellationToken cancellationToken)
        {
            var result = new PushTaskResult { UserStoryId = userStory.Id };

            try
            {
                // Check if UserStory already has a ClickUp task ID
                if (!string.IsNullOrWhiteSpace(userStory.JiraTicket))
                {
                    // Update existing task
                    var updatedTask = await _clickUpService.UpdateTaskAsync(
                        accessToken,
                        userStory.JiraTicket,
                        title: userStory.Title,
                        description: BuildClickUpTaskDescription(userStory),
                        cancellationToken);

                    result.ClickUpTaskId = updatedTask.Id;
                    result.Action = PushAction.Updated;
                    result.Success = true;
                    result.Message = "Task updated successfully";

                    _logger.LogInformation("Updated ClickUp task {TaskId} for UserStory {UserStoryId}",
                        updatedTask.Id, userStory.Id);
                }
                else
                {
                    // Create new task
                    var createdTask = await _clickUpService.CreateTaskAsync(
                        accessToken,
                        listId,
                        title: userStory.Title,
                        description: BuildClickUpTaskDescription(userStory),
                        cancellationToken);

                    // Store the ClickUp task ID in JiraTicket field for future reference
                    userStory.SetClickUpTaskId(createdTask.Id);

                    result.ClickUpTaskId = createdTask.Id;
                    result.Action = PushAction.Created;
                    result.Success = true;
                    result.Message = "Task created successfully";

                    _logger.LogInformation("Created new ClickUp task {TaskId} for UserStory {UserStoryId}",
                        createdTask.Id, userStory.Id);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to push UserStory {UserStoryId}", userStory.Id);
                result.Action = PushAction.Failed;
                result.Success = false;
                result.Message = ex.Message;
                return result;
            }
        }

        /// <summary>
        /// Builds a formatted description for ClickUp task from UserStory
        /// </summary>
        private static string BuildClickUpTaskDescription(UserStory userStory)
        {
            var description = userStory.Description ?? "";

            if (userStory.AcceptanceCriteria != null && userStory.AcceptanceCriteria.Any())
            {
                //var criteria = string.Join("\n- ", userStory.AcceptanceCriteria);

                var criteria = string.Join("\n- ", userStory.AcceptanceCriteria.Select(ac => ac.Text));
                description += $"\n\n### Acceptance Criteria\n- {criteria}";
            }

            if (userStory.Status != null)
            {
                description += $"\n\n**Status**: {userStory.Status}";
            }

            if (userStory.Priority != null)
            {
                description += $"\n**Priority**: {userStory.Priority}";
            }

            return description;
        }
    }
}
