namespace Requra.Infrastructure.Services.ClickUpService
{
    /// <summary>
    /// Service for pushing Requra UserStories to ClickUp
    /// </summary>
    public interface IClickUpPushService
    {
        /// <summary>
        /// Pushes all UserStories from a project to ClickUp
        /// Creates new tasks for UserStories without ClickUp IDs, updates existing ones
        /// </summary>
        Task<PushProjectTasksResult> PushProjectTasksAsync(Guid projectId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Pushes only approved UserStories from a project to ClickUp
        /// </summary>
        Task<PushProjectTasksResult> PushApprovedTasksAsync(Guid projectId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Pushes a single UserStory to ClickUp
        /// </summary>
        Task<PushTaskResult> PushTaskAsync(Guid projectId, Guid userStoryId, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Result of pushing a single task
    /// </summary>
    public class PushTaskResult
    {
        public Guid UserStoryId { get; set; }
        public string? ClickUpTaskId { get; set; }
        public PushAction Action { get; set; }
        public bool Success { get; set; }
        public string? Message { get; set; }
    }

    /// <summary>
    /// Result of pushing all project tasks
    /// </summary>
    public class PushProjectTasksResult
    {
        public Guid ProjectId { get; set; }
        public int CreatedCount { get; set; }
        public int UpdatedCount { get; set; }
        public int FailedCount { get; set; }
        public int SkippedCount { get; set; }
        public List<PushTaskResult> Details { get; set; } = new();
        public string? Message { get; set; }

        public int TotalCount => CreatedCount + UpdatedCount + FailedCount + SkippedCount;
    }

    /// <summary>
    /// Type of action performed during push
    /// </summary>
    public enum PushAction
    {
        Created,
        Updated,
        Skipped,
        Failed
    }
}
