using DocumentFormat.OpenXml.InkML;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Requra.Application.DTOs.AI;
using Requra.Application.Interfaces.IAIService;
using Requra.Domain.Entities;
using Requra.Domain.Enums;
using Requra.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Requra.Infrastructure.Services.JobPollingService
{
    public class JobPollingService(IServiceScopeFactory _scopeFactory, ILogger<JobPollingService> _logger,RequraDbContext _context) : IJobPollingService
    {
        public async Task PollUntilFinishedAsync(Guid runId, string jobId, int maxAttempts = 1000)
        {
            const int delayMs = 3000;
            int attempts = 0;

            while (attempts < maxAttempts)
            {
                try
                {
                    await Task.Delay(delayMs);

                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<RequraDbContext>();
                    var aiClient = scope.ServiceProvider.GetRequiredService<IAIClient>();

                    var statusResponse = await aiClient.GetStatusAsync(jobId);

                    var run = await db.AnalysisRuns.FindAsync(runId);
                    if (run == null) 
                        return;

                    run.UpdateAnalysis(
                        MapStatus(statusResponse.Status),
                        statusResponse.ProgressPct,
                        statusResponse.CurrentNode,
                        statusResponse.Error,
                        run.StartedAt,
                        statusResponse.CompletedAt.HasValue
                            ? DateTimeOffset
                                .FromUnixTimeSeconds((long)statusResponse.CompletedAt.Value)
                                .AddSeconds(statusResponse.CompletedAt.Value % 1)
                                .UtcDateTime
                            : null
                    );

                    if (statusResponse.Status == "COMPLETED" || statusResponse.Status == "PARTIAL")
                    {
                        var result = await aiClient.GetResultAsync(jobId);

                        var rawJson = JsonSerializer.Serialize(result);
                        db.AnalysisResults.Add(new AnalysisResult
                        {
                            AnalysisRunId = runId,
                            RawJson = rawJson,
                            CreatedAt = DateTime.UtcNow
                        });
                        //await MapRequirementsFromAiResultAsync(
                        //        rawJson,
                        //        run.ProjectId
                        //        );
                        //await MapUserStoriesFromAiResultAsync(
                        //        rawJson,
                        //        run.ProjectId
                        //        ); //will add Creator Id later



                        run.UpdateAnalysis(
                            MapStatus(statusResponse.Status),
                            statusResponse.ProgressPct,
                            statusResponse.CurrentNode,
                            null,
                            run.StartedAt,
                            DateTime.UtcNow
                        );

                        await db.SaveChangesAsync();
                        return;
                    }

                    if (statusResponse.Status == "FAILED")
                    {
                        run.UpdateAnalysis(
                            AnalysisRunStatus.FAILED,
                            statusResponse.ProgressPct,
                            statusResponse.CurrentNode,
                            statusResponse.Error ?? "Unknown error",
                            run.StartedAt,
                            DateTime.UtcNow
                        );
                        run.Status = AnalysisRunStatus.FAILED;
                        run.ErrorMessage = statusResponse.Error ?? "Unknown error";

                        await db.SaveChangesAsync();
                        return;
                    }

                    if (statusResponse.Status == "CANCELLED")
                    {
                        run.UpdateAnalysis(
                            AnalysisRunStatus.CANCELLED,
                            statusResponse.ProgressPct,
                            statusResponse.CurrentNode,
                            null,
                            run.StartedAt,
                            DateTime.UtcNow
                        );

                        await db.SaveChangesAsync();
                        return;
                    }

                    await db.SaveChangesAsync();
                    attempts++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error polling status for job {jobId}. Attempt {attempts + 1}/{maxAttempts}");
                    attempts++;
                    if (attempts >= maxAttempts)
                    {
                        throw;
                    }
                }
            }

            // If we reach here, the job timed out
            using (var scope = _scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<RequraDbContext>();
                var timedOutRun = await db.AnalysisRuns.FirstOrDefaultAsync(r => r.Id == runId);
                if (timedOutRun != null)
                {
                    timedOutRun.Status = AnalysisRunStatus.FAILED;
                    timedOutRun.ErrorMessage = "Job processing timeout";
                    timedOutRun.CompletedAt = DateTime.UtcNow;
                    timedOutRun.UpdatedAt = DateTime.UtcNow;
                    await db.SaveChangesAsync();
                }
            }
        }

        private static AnalysisRunStatus MapStatus(string status)
        {
            return status switch
            {
                "COMPLETED" => AnalysisRunStatus.COMPLETED,
                "PARTIAL" => AnalysisRunStatus.PARTIAL,
                "PROCESSING" => AnalysisRunStatus.PROCESSING,
                "FAILED" => AnalysisRunStatus.FAILED,
                "CANCELLED" => AnalysisRunStatus.CANCELLED,
                "QUEUED" => AnalysisRunStatus.QUEUED,
                _ => AnalysisRunStatus.QUEUED
            };
        }

        private async Task MapRequirementsFromAiResultAsync(string rawJson,Guid projectId,CancellationToken cancellationToken = default)
        {
            var aiResult = JsonSerializer.Deserialize<ResultDto>(
                rawJson,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (aiResult?.Requirements == null || aiResult.Requirements.Count == 0)
                return;

            foreach (var aiRequirement in aiResult.Requirements)
            {
                if (string.IsNullOrWhiteSpace(aiRequirement.Id))
                    continue;

                if (string.IsNullOrWhiteSpace(aiRequirement.Title))
                    continue;

                var alreadyExists = await _context.Requirements
                    .AnyAsync(
                        x => x.ProjectId == projectId && x.SourceRequirementId == aiRequirement.Id,
                        cancellationToken);

                if (alreadyExists)
                    continue;

                var requirementType = MapRequirementType(aiRequirement.Type);

                var qualityIssues = SerializeList(aiRequirement.Quality?.Issues);
                var qualityWarnings = SerializeList(aiRequirement.Quality?.Warnings);

                var requirement = new Requirement(
                    sourceRequirementId: aiRequirement.Id,
                    title: aiRequirement.Title,
                    description: aiRequirement.Description,
                    type: requirementType,
                    projectId: projectId,
                    confidenceScore: aiRequirement.ConfidenceScore,
                    qualityScore: aiRequirement.Quality?.Score,
                    qualityIssues: qualityIssues,
                    qualityWarnings: qualityWarnings,
                    deduplicationKey: aiRequirement.DeduplicationKey,
                    actor: aiRequirement.Actor,
                    category: aiRequirement.Category,
                    priority: aiRequirement.Priority
                );

                foreach (var sourceReference in aiRequirement.SourceRefs ?? Enumerable.Empty<RequirementSourceRefDto>())
                {
                    var reference = new RequirementSourceReference(
                        page: sourceReference.Page,
                        quote: sourceReference.Quote,
                        chunkId: sourceReference.ChunkId,
                        sourceId: sourceReference.SourceId,
                        sourceType: sourceReference.SourceType,
                        documentName: sourceReference.DocumentName,
                        confidenceScore: sourceReference.ConfidenceScore
                    );

                    requirement.AddSourceReference(reference);
                }

                _context.Requirements.Add(requirement);
            }
        }
        private static string? SerializeList(List<string>? values)
        {
            if (values == null || values.Count == 0)
                return null;

            return JsonSerializer.Serialize(values);
        }
        private static RequirementType MapRequirementType(string? type)
        {
            return type?.Trim().ToLowerInvariant() switch
            {
                "functional" =>
                    RequirementType.Functional,

                "non-functional" =>
                    RequirementType.Non_Functional,

                "business" =>
                    RequirementType.Business_Rule,

                _ => throw new ArgumentException(
                    $"Unknown requirement type: {type}")
            };
        }

        private async Task MapUserStoriesFromAiResultAsync(
    string rawJson,
    Guid projectId,
    string? creatorId = null,
    CancellationToken cancellationToken = default)
        {
            var aiResult = JsonSerializer.Deserialize<JobResultResponseDto>(
                rawJson,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (aiResult?.UserStories == null || aiResult.UserStories.Count == 0)
                return;

            foreach (var aiUserStory in aiResult.UserStories)
            {
                // Validate User Story
                if (string.IsNullOrWhiteSpace(aiUserStory.Id))
                    continue;

                if (string.IsNullOrWhiteSpace(aiUserStory.Title))
                    continue;

                // Check if User Story already exists
                var alreadyExists = await _context.UserStories
                    .AnyAsync(
                        x => x.ProjectId == projectId &&
                             x.SourceUserStoryId == aiUserStory.Id,
                        cancellationToken);

                if (alreadyExists)
                    continue;

                // Find the Requirement using the AI source ID.
                // Example:
                // AI -> "requirement_id": "REQ-001"
                // DB -> Requirement.SourceRequirementId = "REQ-001"
                var requirement = await _context.Requirements
                    .FirstOrDefaultAsync(
                        x => x.ProjectId == projectId &&
                             x.SourceRequirementId == aiUserStory.RequirementId,
                        cancellationToken);

                if (requirement == null)
                    continue;

                // Map AI values to domain enums
                var priority = MapUserStoryPriority(aiUserStory.Priority);

                var type = MapUserStoryType(aiUserStory.Type);

                // New User Stories start with NeedReview status
                var status = UserStoryStatus.NeedReview;

                // Map Acceptance Criteria
                var acceptanceCriteria = new List<AcceptanceCriterion>();

                foreach (var aiCriterion in
                         aiUserStory.AcceptanceCriteria ??
                         Enumerable.Empty<AcceptanceCriteriaDto>())
                {
                    if (string.IsNullOrWhiteSpace(aiCriterion.Text))
                        continue;

                    var criterion = new AcceptanceCriterion(
                        sourceAcceptanceCriterionId: aiCriterion.Id,
                        text: aiCriterion.Text,
                        criterionType: aiCriterion.CriterionType
                    );

                    acceptanceCriteria.Add(criterion);
                }

                // Create User Story
                // The AI "user_story" is stored in our Description field.
                var userStory = new UserStory(
                    sourceUserStoryId: aiUserStory.Id,
                    title: aiUserStory.Title,
                    description: aiUserStory.UserStory,
                    acceptanceCriteria: acceptanceCriteria,
                    type: type,
                    status: status,
                    priority: priority,
                    language: null,
                    creatorId: creatorId,
                    requirementId: requirement.Id,
                    projectId: projectId,
                    storyPoints: aiUserStory.JiraFields?.StoryPoints,
                    sourceRequirementId: aiUserStory.RequirementId,
                    deduplicationKey: aiUserStory.DeduplicationKey
                );

                // Map Source References
                foreach (var sourceReference in
                         aiUserStory.SourceRefs ??
                         Enumerable.Empty<UserStorySourceRefDto>())
                {
                    var reference = new UserStorySourceRef(
                        page: sourceReference.Page,
                        quote: sourceReference.Quote,
                        chunkId: sourceReference.ChunkId,
                        sourceId: sourceReference.SourceId,
                        sourceType: sourceReference.SourceType,
                        documentName: sourceReference.DocumentName,
                        confidenceScore: sourceReference.ConfidenceScore
                    );

                    userStory.AddSourceReference(reference);
                }

                _context.UserStories.Add(userStory);
            }
        }

        private static UserStoryPriority MapUserStoryPriority(string? priority)
        {
            return priority?.Trim().ToLowerInvariant() switch
            {
                "low" => UserStoryPriority.low,
                "medium" => UserStoryPriority.medium,
                "high" => UserStoryPriority.high,
                "critical" => UserStoryPriority.critical,

                _ => UserStoryPriority.medium
            };
        }

        private static UserStoryType MapUserStoryType(string? type)
        {
            return type?.Trim().ToLowerInvariant() switch
            {
                "functional" => UserStoryType.Functional,
                "non-functional" => UserStoryType.NonFunctional,

                _ => throw new ArgumentException(
                    $"Unknown user story type: {type}")
            };
        }
    }
    
}
