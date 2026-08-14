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
                        await MapRequirementsFromAiResultAsync(
                                rawJson,
                                run.ProjectId
                                );

                        // Map AI results to database entities
                        await MapAIResultsToEntitiesAsync(db, run, result);

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

        private async Task MapAIResultsToEntitiesAsync(RequraDbContext db, AnalysisRun run, Application.DTOs.AI.JobResultResponseDto result)
        {
            try
            {
                if (run.ProjectId == null)
                    return;

                // Dictionary to map AI Requirement IDs to our database Requirement IDs
                var aiRequirementIdToDbId = new Dictionary<string, Guid>();

                // Map Requirements from AI result to database entities
                //if (result.Requirements != null && result.Requirements.Count > 0)
                //{
                //    foreach (var reqDto in result.Requirements)
                //    {
                //        // Check if requirement already exists by title
                //        var existingReq = await db.Requirements
                //            .FirstOrDefaultAsync(r => r.ProjectId == run.ProjectId && r.Title == reqDto.Title);

                //        Guid dbRequirementId;

                //        if (existingReq == null)
                //        {
                //            // Create new requirement using constructor
                //            var newReq = new Requirement(
                //                reqDto.Title,
                //                Domain.Enums.RequirementType.Functional,
                //                null
                //            );

                //            // Use UpdateDetails to set description
                //            newReq.UpdateDetails(
                //                reqDto.Title,
                //                reqDto.Description,
                //                Domain.Enums.RequirementType.Functional,
                //                null
                //            );

                //            // Mark as approved based on deduplication key presence
                //            if (!string.IsNullOrEmpty(reqDto.DeduplicationKey))
                //            {
                //                newReq.Approve();
                //            }

                //            db.Requirements.Add(newReq);
                //            await db.SaveChangesAsync();
                //            dbRequirementId = newReq.Id;
                //        }
                //        else
                //        {
                //            dbRequirementId = existingReq.Id;

                //            // Update approval status if needed
                //            if (!string.IsNullOrEmpty(reqDto.DeduplicationKey) &&
                //                existingReq.Status != Domain.Enums.RequirementStatus.Approved)
                //            {
                //                existingReq.Approve();
                //                await db.SaveChangesAsync();
                //            }
                //        }

                //        // Map AI ID to our database ID
                //        if (!aiRequirementIdToDbId.ContainsKey(reqDto.Id))
                //        {
                //            aiRequirementIdToDbId[reqDto.Id] = dbRequirementId;
                //        }
                //    }
                //}

                // Map User Stories from AI result to database entities
                if (result.UserStories != null && result.UserStories.Count > 0)
                {
                    // Get a system user ID for the creator
                    var creatorId = "system"; // Placeholder - adjust based on your actual user ID

                    foreach (var storyDto in result.UserStories)
                    {
                        // Check if user story already exists
                        var existingStory = await db.UserStories
                            .FirstOrDefaultAsync(us => us.ProjectId == run.ProjectId && us.Title == storyDto.Title);

                        // Find associated requirement using the mapped ID
                        Guid? requirementId = null;

                        if (!string.IsNullOrEmpty(storyDto.RequirementId) &&
                            aiRequirementIdToDbId.TryGetValue(storyDto.RequirementId, out var dbReqId))
                        {
                            requirementId = dbReqId;
                        }
                        else
                        {
                            // Fallback: try to find requirement by matching with AI response
                            var aiRequirement = result.Requirements?
                                .FirstOrDefault(r => r.Id == storyDto.RequirementId);

                            if (aiRequirement != null)
                            {
                                // Find our database requirement by title
                                var dbRequirement = await db.Requirements
                                    .FirstOrDefaultAsync(r => r.ProjectId == run.ProjectId &&
                                                              r.Title == aiRequirement.Title);

                                if (dbRequirement != null)
                                {
                                    requirementId = dbRequirement.Id;
                                }
                            }
                        }

                        // Only create user story if we found an associated requirement
                        if (requirementId.HasValue)
                        {
                            if (existingStory == null)
                            {
                                var newStory = new UserStory(
                                    storyDto.Title,
                                    creatorId,
                                    requirementId.Value,
                                    Domain.Enums.UserStoryPriority.medium
                                );

                                // Extract acceptance criteria text
                                var acceptanceCriteria = storyDto.AcceptanceCriteria?
                                    .Select(ac => ac.Text)
                                    .ToList();

                                newStory.UpdateDetails(
                                    storyDto.Title,
                                    storyDto.UserStory,
                                    acceptanceCriteria,
                                    null
                                );

                                // Mark as approved based on deduplication key presence
                                if (!string.IsNullOrEmpty(storyDto.DeduplicationKey))
                                {
                                    newStory.ChangeStatus(Domain.Enums.UserStoryStatus.Approved);
                                }

                                db.UserStories.Add(newStory);
                                await db.SaveChangesAsync();
                            }
                            else if (!string.IsNullOrEmpty(storyDto.DeduplicationKey))
                            {
                                // Update existing story status if it's approved
                                if (existingStory.Status != Domain.Enums.UserStoryStatus.Approved)
                                {
                                    existingStory.ChangeStatus(Domain.Enums.UserStoryStatus.Approved);
                                    await db.SaveChangesAsync();
                                }
                            }
                        }
                        else
                        {
                            _logger.LogWarning(
                                "Could not find associated requirement for user story '{Title}' in project {ProjectId}",
                                storyDto.Title, run.ProjectId);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error mapping AI results to entities for run {RunId}", run.Id);
                // Don't throw - let the process continue even if mapping fails
            }
        }
    }
    
}
