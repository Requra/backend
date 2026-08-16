using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Requra.Domain.Entities;
using Requra.Domain.Enums;
using Requra.Infrastructure.Data;
using Requra.Infrastructure.ExternalDTOs.ClickUpDto;
using Requra.Infrastructure.ExternalInterfaces.IClickUpService;

namespace Requra.Infrastructure.Services.ClickUpService
{
    public interface IClickUpSyncService
    {
        /// <summary>
        /// Syncs all tasks from ClickUp to UserStories for a project
        /// </summary>
        Task<int> SyncProjectTasksAsync(Guid projectId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Syncs tasks from a specific ClickUp list
        /// </summary>
        Task<int> SyncListTasksAsync(Guid projectId, string listId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Syncs a single ClickUp task to a UserStory
        /// </summary>
        Task<UserStory> SyncTaskAsync(Guid projectId, ClickUpTask task, CancellationToken cancellationToken = default);
    }

    public class ClickUpSyncService : IClickUpSyncService
    {
        private readonly RequraDbContext _dbContext;
        private readonly IClickUpService _clickUpService;
        private readonly ILogger<ClickUpSyncService> _logger;

        public ClickUpSyncService(
            RequraDbContext dbContext,
            IClickUpService clickUpService,
            ILogger<ClickUpSyncService> logger)
        {
            _dbContext = dbContext;
            _clickUpService = clickUpService;
            _logger = logger;
        }

        public async Task<int> SyncProjectTasksAsync(Guid projectId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Starting sync for project {ProjectId}", projectId);

                var project = await _dbContext.Projects
                    .FirstOrDefaultAsync(p => p.Id == projectId && p.IsClickUpConnected, cancellationToken);

                if (project == null)
                {
                    _logger.LogWarning("Project {ProjectId} not found or not connected to ClickUp", projectId);
                    throw new InvalidOperationException($"Project {projectId} not found or not connected to ClickUp");
                }

                _logger.LogInformation("Project found. ListId: {ListId}, SpaceId: {SpaceId}", project.ClickUpListId, project.ClickUpSpaceId);

                if (project.IsClickUpTokenExpired())
                {
                    _logger.LogWarning("ClickUp token for project {ProjectId} has expired", projectId);
                    project.DisconnectFromClickUp();
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    throw new InvalidOperationException("ClickUp token has expired");
                }

                if (string.IsNullOrWhiteSpace(project.ClickUpAccessToken))
                {
                    _logger.LogWarning("Project {ProjectId} has no access token", projectId);
                    throw new InvalidOperationException("No ClickUp access token found");
                }

                ClickUpTasksResponse? tasksResponse = null;

                // Fetch tasks from list if specified, otherwise from space
                if (!string.IsNullOrWhiteSpace(project.ClickUpListId))
                {
                    _logger.LogInformation("Fetching tasks from ClickUp list {ListId}", project.ClickUpListId);
                    tasksResponse = await _clickUpService.GetListTasksAsync(
                        project.ClickUpAccessToken,
                        project.ClickUpListId,
                        cancellationToken);
                }
                else if (!string.IsNullOrWhiteSpace(project.ClickUpSpaceId))
                {
                    _logger.LogInformation("Fetching tasks from ClickUp space {SpaceId}", project.ClickUpSpaceId);
                    tasksResponse = await _clickUpService.GetSpaceTasksAsync(
                        project.ClickUpAccessToken,
                        project.ClickUpSpaceId,
                        cancellationToken);
                }
                else
                {
                    _logger.LogWarning("Project {ProjectId} has no ClickUp list or space configured", projectId);
                    throw new InvalidOperationException("No ClickUp list or space configured");
                }

                if (tasksResponse?.Tasks == null || !tasksResponse.Tasks.Any())
                {
                    _logger.LogInformation("No tasks found in ClickUp for project {ProjectId}", projectId);
                    return 0;
                }

                _logger.LogInformation("Found {TaskCount} tasks to sync", tasksResponse.Tasks.Count);

                int syncedCount = 0;
                foreach (var task in tasksResponse.Tasks)
                {
                    try
                    {
                        await SyncTaskAsync(projectId, task, cancellationToken);
                        syncedCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to sync ClickUp task {TaskId}", task.Id);
                    }
                }

                try
                {
                    _logger.LogInformation("Saving {ChangedEntityCount} changed entities to database", _dbContext.ChangeTracker.Entries().Count(e => e.State != Microsoft.EntityFrameworkCore.EntityState.Unchanged));
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to save changes to database after syncing tasks. EntityState entries: {EntityCount}",
                        _dbContext.ChangeTracker.Entries().Count());

                    // Log all changed entities for debugging
                    foreach (var entry in _dbContext.ChangeTracker.Entries().Where(e => e.State != Microsoft.EntityFrameworkCore.EntityState.Unchanged))
                    {
                        _logger.LogError("Entity {EntityType} in state {EntityState}: {EntityKey}",
                            entry.Entity.GetType().Name,
                            entry.State,
                            entry.Entity);
                    }

                    throw;
                }

                _logger.LogInformation("Successfully synced {SyncedCount} tasks for project {ProjectId}", syncedCount, projectId);
                return syncedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing project tasks from ClickUp for project {ProjectId}", projectId);
                throw;
            }
        }

        public async Task<int> SyncListTasksAsync(Guid projectId, string listId, CancellationToken cancellationToken = default)
        {
            try
            {
                var project = await _dbContext.Projects
                    .FirstOrDefaultAsync(p => p.Id == projectId && p.IsClickUpConnected, cancellationToken);

                if (project == null)
                {
                    _logger.LogWarning("Project {ProjectId} not found or not connected to ClickUp", projectId);
                    return 0;
                }

                if (string.IsNullOrWhiteSpace(project.ClickUpAccessToken))
                    return 0;

                var tasksResponse = await _clickUpService.GetListTasksAsync(
                    project.ClickUpAccessToken,
                    listId,
                    cancellationToken);

                if (tasksResponse?.Tasks == null || !tasksResponse.Tasks.Any())
                    return 0;

                int syncedCount = 0;
                foreach (var task in tasksResponse.Tasks)
                {
                    try
                    {
                        await SyncTaskAsync(projectId, task, cancellationToken);
                        syncedCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to sync ClickUp task {TaskId}", task.Id);
                    }
                }

                await _dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Successfully synced {SyncedCount} tasks from list {ListId} for project {ProjectId}", 
                    syncedCount, listId, projectId);
                return syncedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing list tasks from ClickUp");
                throw;
            }
        }

        public async Task<UserStory> SyncTaskAsync(Guid projectId, ClickUpTask task, CancellationToken cancellationToken = default)
        {
            try
            {
                // Check if UserStory already exists with this ClickUp task ID
                var existingUserStory = await _dbContext.UserStories
                    .FirstOrDefaultAsync(u => u.ProjectId == projectId && u.JiraTicket == task.Id, cancellationToken);

                // Map ClickUp priority to UserStoryPriority
                var priority = MapClickUpPriorityToUserStoryPriority(task.Priority?.Priority);

                // Map ClickUp status to UserStoryStatus
                var status = MapClickUpStatusToUserStoryStatus(task.Status?.Status);

                if (existingUserStory != null)
                {
                    // Update existing UserStory
                    existingUserStory.UpdateDetails(
                        title: task.Name,
                        description: task.Description,
                        priority: priority,
                        status: status
                    );

                    _logger.LogInformation("Updated UserStory {UserStoryId} from ClickUp task {TaskId}", 
                        existingUserStory.Id, task.Id);
                    return existingUserStory;
                }

                // Create new UserStory from ClickUp task
                // Note: We need a requirement and creator ID - using defaults or fetching from project context
                var requirement = await _dbContext.Requirements
                    .FirstOrDefaultAsync(r => r.ProjectId == projectId, cancellationToken);

                if (requirement == null)
                {
                    _logger.LogWarning("No requirement found for project {ProjectId}, creating UserStory without requirement", projectId);
                    throw new InvalidOperationException($"No requirement found for project {projectId}");
                }

                // Get a project member as creator (default to first member)
                var projectMember = await _dbContext.ProjectMembers
                    .FirstOrDefaultAsync(m => m.ProjectId == projectId, cancellationToken);

                if (projectMember == null)
                {
                    _logger.LogWarning("No project member found for project {ProjectId}", projectId);
                    throw new InvalidOperationException($"No project member found for project {projectId}");
                }

                var newUserStory = new UserStory(
                    title: task.Name,
                    creatorId: projectMember.UserId,
                    requirementId: requirement.Id,
                    priority: priority,
                    projectId: projectId
                );

                newUserStory.SetDescription(task.Description);
                newUserStory.ChangeStatus(status);
                newUserStory.SetClickUpTaskId(task.Id);

                _dbContext.UserStories.Add(newUserStory);
                _logger.LogInformation("Created new UserStory from ClickUp task {TaskId}", task.Id);
                return newUserStory;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing ClickUp task {TaskId} to UserStory", task.Id);
                throw;
            }
        }

        private UserStoryPriority MapClickUpPriorityToUserStoryPriority(string? clickUpPriority)
        {
            return clickUpPriority?.ToLowerInvariant() switch
            {
                "urgent" => UserStoryPriority.critical,
                "high" => UserStoryPriority.high,
                "medium" => UserStoryPriority.medium,
                "low" => UserStoryPriority.low,
                _ => UserStoryPriority.medium
            };
        }

        private UserStoryStatus MapClickUpStatusToUserStoryStatus(string? clickUpStatus)
        {
            return clickUpStatus?.ToLowerInvariant() switch
            {
                "open" => UserStoryStatus.Approved,
                "in progress" => UserStoryStatus.Approved,
                "in_progress" => UserStoryStatus.Approved,
                "done" => UserStoryStatus.Approved,
                "closed" => UserStoryStatus.Approved,
                "todo" => UserStoryStatus.NeedReview,
                _ => UserStoryStatus.NeedReview
            };
        }

        private List<string> ExtractAcceptanceCriteria(string? description)
        {
            if (string.IsNullOrWhiteSpace(description))
                return new List<string>();

            // Simple extraction: split by common delimiters
            var lines = description.Split(new[] { "\n", "\r\n", ";" }, StringSplitOptions.RemoveEmptyEntries);
            return lines
                .Where(line => !string.IsNullOrWhiteSpace(line) && line.Length > 5)
                .Take(5)
                .Select(line => line.Trim())
                .ToList();
        }
    }
}
