using CloudinaryDotNet.Actions;
using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Office2010.Word;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Requra.Application.DTOs;
using Requra.Application.DTOs.AI;
using Requra.Application.DTOs.ProjectMembers;
using Requra.Application.Interfaces.IAIService;
using Requra.Application.Interfaces.IAnalysisRunService;
using Requra.Application.Interfaces.IAnalysisRunWorker;
using Requra.Application.Interfaces.IDocumentService;
using Requra.Application.Interfaces.IFileDownloader;
using Requra.Application.Response;
using Requra.Domain.Entities;
using Requra.Domain.Enums;
using Requra.Infrastructure.Data;
using Requra.Infrastructure.Helpers;
using Requra.Infrastructure.Http.FileDownloader;
using Requra.Infrastructure.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Requra.Infrastructure.Services.AnalysisRunService
{
    public class AnalysisRunService(ILogger<AnalysisRunService> _logger, IAnalysisRunWorker _worker, IServiceScopeFactory _scopeFactory, RequraDbContext dbContext, IAIClient aiClient, IDocumentService documentService, IFileDownloader fileDownloader, IJobPollingService jobPollingService, IExcelExportService excelExportService) : IAnalysisRunService
    {



        public async Task<Response<AnalysisRunDto>> StartRunAsync(StartRunRequest request, Guid projectId, string userId)
        {
            var project = await dbContext.Projects.FindAsync(projectId);
            if (project == null)
            {
                return Response<AnalysisRunDto>.Failure("Project not found", 404);
            }

            var isMember = await dbContext.ProjectMembers
                .AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == userId);

            if (!isMember)
            {
                return Response<AnalysisRunDto>.Failure(
                    null,
                    "You are not allowed to access this project or Start a new run",
                    403);
            }

            List<Document> documents;
            if (request.DocumentIds != null && request.DocumentIds.Any())
            {
                documents = await dbContext.Documents
                    .Where(d => request.DocumentIds.Contains(d.Id) && d.ProjectId == projectId)
                    .ToListAsync();

                if (documents.Count != request.DocumentIds.Count)
                {
                    return Response<AnalysisRunDto>.Failure(
                        "One or more documents not found or do not belong to the project",
                        404);
                }
            }
            else
            {
                documents = await dbContext.Documents
                    .Where(d => d.ProjectId == projectId)
                    .ToListAsync();
            }

            List<MeetingSession> meetingSessions;
            if (request.MeetingIds != null && request.MeetingIds.Any())
            {
                meetingSessions = await dbContext.MeetingSessions
                    .Where(ms => request.MeetingIds.Contains(ms.Id) && ms.ProjectId == projectId)
                    .ToListAsync();

                if (meetingSessions.Count != request.MeetingIds.Count)
                {
                    return Response<AnalysisRunDto>.Failure(
                        "One or more meetings not found or do not belong to the project",
                        404);
                }
            }
            else
            {
                meetingSessions = await dbContext.MeetingSessions
                    .Where(ms => ms.ProjectId == projectId)
                    .ToListAsync();
            }

            var hasDocuments = documents.Any();
            var hasMeetings = meetingSessions.Any();

            if (!hasDocuments && !hasMeetings)
            {
                return Response<AnalysisRunDto>.Failure(
                    "Project has no documents or meetings to analyze",
                    400);
            }

            var categories = documents
                .Select(d => DocumentTypeHelper.GetCategory(d.Type))
                .Distinct()
                .ToList();

            if (categories.Contains("unknown"))
            {
                return Response<AnalysisRunDto>.Failure(
                    "Unsupported file type detected",
                    400);
            }

            //if (categories.Count > 1)
            //{
            //    return Response<AnalysisRunDto>.Failure(
            //        "Cannot mix audio files with documents in the same run",
            //        400);
            //}

            var activeRun = await dbContext.AnalysisRuns
                .Where(x => x.ProjectId == projectId &&
                       (x.Status == AnalysisRunStatus.QUEUED ||
                        x.Status == AnalysisRunStatus.PROCESSING))
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();

            var finalDocumentIds = (request.DocumentIds != null && request.DocumentIds.Any())
                ? request.DocumentIds
                : documents.Select(d => d.Id).ToList();

            var finalMeetingIds = (request.MeetingIds != null && request.MeetingIds.Any())
                ? request.MeetingIds
                : meetingSessions.Select(ms => ms.Id).ToList();

            if (activeRun != null)
            {
                return Response<AnalysisRunDto>.Success(
                    new AnalysisRunDto
                    {
                        Id = activeRun.Id,
                        ProjectId = activeRun.ProjectId,
                        Status = activeRun.Status,
                        Progress = activeRun.Progress,
                        CurrentNode = activeRun.CurrentNode,
                        CurrentNodeLabel = PipelineNodeMapper.GetLabel(activeRun.CurrentNode),
                        ErrorMessage = activeRun.ErrorMessage,
                        DocumentIds = finalDocumentIds,
                        MeetingIds = finalMeetingIds,
                        CreatedAt = activeRun.CreatedAt,
                        UpdatedAt = activeRun.UpdatedAt,
                        StartedAt = activeRun.StartedAt,
                        CompletedAt = activeRun.CompletedAt
                    },
                    "An active analysis run already exists for this project",
                    200
                );
            }

            var run = new AnalysisRun
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Status = AnalysisRunStatus.QUEUED,
                CurrentNode = "queued",
                CreatedAt = DateTime.UtcNow,
                Progress = 0,
                UpdatedAt = DateTime.UtcNow
            };

            dbContext.AnalysisRuns.Add(run);
            await dbContext.SaveChangesAsync();

            var safeFiles = new List<FileUploadDto>();

            // Add documents
            foreach (var doc in documents)
            {
                if (string.IsNullOrWhiteSpace(doc.StorageUrl))
                    continue;

                var bytes = await fileDownloader.DownloadAsync(doc.StorageUrl);

                safeFiles.Add(new FileUploadDto
                {
                    Content = bytes,
                    FileName = string.IsNullOrWhiteSpace(doc.Title)
                        ? $"document-{doc.Id}"
                        : doc.Title
                });
            }

            // Add meeting recordings
            foreach (var meeting in meetingSessions)
            {
                if (meeting.RecordingUrls == null || !meeting.RecordingUrls.Any())
                    continue;

                var validUrls = meeting.RecordingUrls
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var index = 1;

                foreach (var recordingUrl in validUrls)
                {
                    var bytes = await fileDownloader.DownloadAsync(recordingUrl);

                    safeFiles.Add(new FileUploadDto
                    {
                        Content = bytes,
                        FileName = !string.IsNullOrWhiteSpace(meeting.Title)
                            ? $"{meeting.Title}-recording-{index}"
                            : $"meeting-{meeting.Id}-recording-{index}"
                    });

                    index++;
                }
            }

            if (!safeFiles.Any())
            {
                return Response<AnalysisRunDto>.Failure(
                    "No downloadable document or meeting recording files were found for this run.",
                    400);
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    await _worker.ProcessRun(safeFiles, run.Id, projectId);
                }
                catch (Exception ex)
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<RequraDbContext>();

                    var failedRun = await db.AnalysisRuns.FindAsync(run.Id);
                    if (failedRun != null)
                    {
                        failedRun.Status = AnalysisRunStatus.FAILED;
                        failedRun.ErrorMessage = ex.Message;
                        failedRun.UpdatedAt = DateTime.UtcNow;

                        await db.SaveChangesAsync();
                    }
                }
            });

            return Response<AnalysisRunDto>.Success(
                new AnalysisRunDto
                {
                    Id = run.Id,
                    ProjectId = run.ProjectId,
                    Status = run.Status,
                    Progress = run.Progress,
                    CurrentNode = run.CurrentNode,
                    CurrentNodeLabel = PipelineNodeMapper.GetLabel(run.CurrentNode),
                    ErrorMessage = run.ErrorMessage,
                    DocumentIds = finalDocumentIds,
                    MeetingIds = finalMeetingIds,
                    StartedAt = run.StartedAt,
                    CompletedAt = run.CompletedAt,
                    CreatedAt = run.CreatedAt,
                    UpdatedAt = run.UpdatedAt
                },
                "AI analysis run created successfully",
                200
            );
        }

        public async Task<Response<AnalysisRunDto>> GetRunAsync(Guid projectId, Guid runId, string userId)
        {
            if(runId == Guid.Empty)
                return Response<AnalysisRunDto?>.Failure(new(), "Invalid run ID.", 400); //will be removed may be
            var analysisRun = await dbContext.AnalysisRuns.FirstOrDefaultAsync(r => r.Id == runId);
            if (analysisRun == null)
                return Response<AnalysisRunDto?>.Failure(new(), "Run not found.", 404);
       
            var project = await dbContext.Projects.FindAsync(projectId);

            if (project == null)
                return Response<AnalysisRunDto?>.Failure(new(), "Project not found.", 404);
            var isMember = await dbContext.ProjectMembers.AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == userId);
            if (!isMember)
                return Response<AnalysisRunDto?>.Failure(null, "You are not allowed to access this project or see its run status.", 403);
            if (project.Id != analysisRun.ProjectId)
                return Response<AnalysisRunDto?>.Failure(new(), "Run does not belong to the specified project.", 400);

            var documents = await dbContext.Documents
                .Where(d => d.ProjectId == analysisRun.ProjectId)
                .Select(d => d.Id)
                .ToListAsync();
            return Response<AnalysisRunDto>.Success(

                 new AnalysisRunDto
                 {
                     Id = analysisRun.Id,
                     ProjectId = analysisRun.ProjectId,
                     Status = analysisRun.Status,
                     Progress = analysisRun.Progress,
                     CurrentNode = analysisRun.CurrentNode, //Will be enum
                     CurrentNodeLabel = PipelineNodeMapper.GetLabel(analysisRun.CurrentNode),
                     //job_id will be added later "Inicates job id of the current agent"
                     DocumentIds = documents,
                     //MeetingId will be Ignored now
                     ErrorMessage = analysisRun.ErrorMessage,
                     CreatedAt = analysisRun.CreatedAt,
                     UpdatedAt = analysisRun.UpdatedAt,
                     StartedAt = analysisRun.StartedAt,
                     CompletedAt = analysisRun.CompletedAt
                 },
                  "Analysis run status retrieved successfully",
                  200
              );


        }

        public async Task<Response<JobResultResponseDto?>> GetResultAsync(Guid projectId, Guid? runId, string userId)
        {
            var project = await dbContext.Projects.FindAsync(projectId);
            if (project == null)
                return Response<JobResultResponseDto?>.Failure(new(), "Project not found.", 404);

            var isMember = await dbContext.ProjectMembers.AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == userId);
            if (!isMember)
                return Response<JobResultResponseDto?>.Failure(null, "You are not allowed to access this project or Get its AI results.", 403);

            AnalysisRun? run;

            if (runId.HasValue)
            {
                run = await dbContext.AnalysisRuns
                    .FirstOrDefaultAsync(r => r.Id == runId.Value);
            }
            else
            {
                // Get latest COMPLETED or PARTIAL run
                run = await dbContext.AnalysisRuns
                    .Where(r => r.ProjectId == projectId && (r.Status == AnalysisRunStatus.COMPLETED || r.Status == AnalysisRunStatus.PARTIAL))
                    .OrderByDescending(r => r.CompletedAt)
                    .FirstOrDefaultAsync();
            }

            if (run == null)
                return Response<JobResultResponseDto?>.Failure(new(), "Run not found.", 404);

            if (run.ProjectId != projectId)
                return Response<JobResultResponseDto?>.Failure(new(), "Run does not belong to the specified project.", 400);

            if (run.Status == AnalysisRunStatus.QUEUED ||
                run.Status == AnalysisRunStatus.PROCESSING)
            {
                return Response<JobResultResponseDto?>.Success(
                    new(),
                    "Result is not ready yet."
                );
            }

            if (run.Status == AnalysisRunStatus.FAILED)
            {
                return Response<JobResultResponseDto?>.Failure(
                    new(),
                    run.ErrorMessage ?? "Analysis failed.",
                    200
                );
            }

            var result = await dbContext.AnalysisResults
                .FirstOrDefaultAsync(r => r.AnalysisRunId == run.Id);

            if (result == null)
            {
                return Response<JobResultResponseDto?>.Failure(
                    new(),
                    "Run completed but result is missing.",
                    500
                );
            }

            JobResultResponseDto aiData;

            try
            {
                aiData = JsonSerializer.Deserialize<JobResultResponseDto>(result.RawJson);
            }
            catch
            {
                return Response<JobResultResponseDto?>.Failure(
                    new(),
                    "Invalid AI JSON format",
                    500
                );
            }

            return Response<JobResultResponseDto?>.Success(
                aiData,
                "Results retrieved successfully"
            );
        }

        public async Task<Response<CancelJobResponseDto>> CancelRunAsync(Guid projectId, Guid runId, string userId)
        {
            // Verify project exists and user is member
            var project = await dbContext.Projects.FindAsync(projectId);
            if (project == null)
                return Response<CancelJobResponseDto>.Failure(null, "Project not found", 404);

            var isMember = await dbContext.ProjectMembers.AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == userId);
            if (!isMember)
                return Response<CancelJobResponseDto>.Failure(null, "You are not allowed to access this project", 403);

            // Verify run exists and belongs to project
            var run = await dbContext.AnalysisRuns.FirstOrDefaultAsync(r => r.Id == runId);
            if (run == null)
                return Response<CancelJobResponseDto>.Failure(null, "Run not found", 404);

            if (run.ProjectId != projectId)
                return Response<CancelJobResponseDto>.Failure(null, "Run does not belong to the specified project", 400);

            try
            {
                // Call AI service to cancel the job using run Id as job id
                var jobId = runId.ToString();
                var cancelResponse = await aiClient.CancelJobAsync(jobId);

                // Update run status based on response
                if (cancelResponse.Cancelled)
                {
                    run.Status = AnalysisRunStatus.CANCELLED;
                    run.UpdatedAt = DateTime.UtcNow;
                    if (run.Status != AnalysisRunStatus.COMPLETED && run.Status != AnalysisRunStatus.PARTIAL)
                    {
                        run.CompletedAt = DateTime.UtcNow;
                    }
                    await dbContext.SaveChangesAsync();
                }

                return Response<CancelJobResponseDto>.Success(cancelResponse, "Job cancellation request processed", 200);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error cancelling job {runId}");
                return Response<CancelJobResponseDto>.Failure(null, "Failed to cancel job", 500);
            }
        }

        public async Task<Response<RetryJobResponseDto>> RetryRunAsync(Guid projectId, Guid runId, string userId)
        {
            // Verify project exists and user is member
            var project = await dbContext.Projects.FindAsync(projectId);
            if (project == null)
                return Response<RetryJobResponseDto>.Failure(null, "Project not found", 404);

            var isMember = await dbContext.ProjectMembers.AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == userId);
            if (!isMember)
                return Response<RetryJobResponseDto>.Failure(null, "You are not allowed to access this project", 403);

            // Verify run exists and belongs to project
            var run = await dbContext.AnalysisRuns.FirstOrDefaultAsync(r => r.Id == runId);
            if (run == null)
                return Response<RetryJobResponseDto>.Failure(null, "Run not found", 404);

            if (run.ProjectId != projectId)
                return Response<RetryJobResponseDto>.Failure(null, "Run does not belong to the specified project", 400);

            try
            {
                // Call AI service to retry the job using run Id as job id
                var jobId = runId.ToString();
                var retryResponse = await aiClient.RetryJobAsync(jobId);

                // Check if retry was successful (status should be QUEUED)
                if (retryResponse.Status == "QUEUED")
                {
                    // Update run to reflect retry
                    run.Status = AnalysisRunStatus.QUEUED;
                    run.Progress = 0;
                    run.ErrorMessage = null;
                    run.UpdatedAt = DateTime.UtcNow;
                    run.StartedAt = DateTime.UtcNow;
                    // Don't reset CompletedAt yet
                    await dbContext.SaveChangesAsync();

                    // Start polling for the retry
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await jobPollingService.PollUntilFinishedAsync(run.Id, jobId);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Error polling retry for run {run.Id}");
                            using var scope = _scopeFactory.CreateScope();
                            var db = scope.ServiceProvider.GetRequiredService<RequraDbContext>();
                            var failedRun = await db.AnalysisRuns.FindAsync(run.Id);
                            if (failedRun != null)
                            {
                                failedRun.Status = AnalysisRunStatus.FAILED;
                                failedRun.ErrorMessage = ex.Message;
                                failedRun.CompletedAt = DateTime.UtcNow;
                                failedRun.UpdatedAt = DateTime.UtcNow;
                                await db.SaveChangesAsync();
                            }
                        }
                    });
                }

                return Response<RetryJobResponseDto>.Success(retryResponse, "Retry request processed", 200);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrying job {runId}");
                return Response<RetryJobResponseDto>.Failure(null, "Failed to retry job", 500);
            }
        }

        public async Task<Response<ExportResultsDto>> ExportResultsDashboardAsync(Guid projectId, Guid runId, string format, string userId)
        {
            // Verify project exists and user is member
            var project = await dbContext.Projects.FindAsync(projectId);
            if (project == null)
                return Response<ExportResultsDto>.Failure(null, "Project not found", 404);

            var isMember = await dbContext.ProjectMembers.AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == userId);
            if (!isMember)
                return Response<ExportResultsDto>.Failure(null, "You are not allowed to access this project", 403);

            // Verify run exists and belongs to project
            var run = await dbContext.AnalysisRuns.FirstOrDefaultAsync(r => r.Id == runId && r.ProjectId == projectId);
            if (run == null)
                return Response<ExportResultsDto>.Failure(null, "Run not found", 404);

            try
            {
                // Validate format
                if (!format.Equals("xlsx", StringComparison.OrdinalIgnoreCase) && !format.Equals("csv", StringComparison.OrdinalIgnoreCase))
                {
                    return Response<ExportResultsDto>.Failure(null, "Invalid format. Allowed formats: xlsx, csv", 400);
                }

                // Get APPROVED requirements and user stories from DATABASE (not from AI result)
                var approvedRequirements = await dbContext.Requirements
                    .Where(r => r.ProjectId == projectId && r.Status == RequirementStatus.Approved)
                    .ToListAsync();

                var approvedUserStories = await dbContext.UserStories
                    .Where(us => us.ProjectId == projectId && us.Status == UserStoryStatus.Approved)
                    .ToListAsync();

                // Map database entities to DTOs for export
                var requirementDtos = approvedRequirements.Select(r => new RequirementDto
                {
                    Id = r.Id.ToString(),
                    Title = r.Title,
                    Description = r.Description,
                    Category = r.Type.ToString(),
                    Type = r.Type.ToString()
                }).ToList();

                var userStoryDtos = approvedUserStories.Select(us => new UserStoryDto
                {
                    Id = us.Id.ToString(),
                    Title = us.Title,
                    UserStory = us.Description,
                    RequirementId = us.RequirementId.ToString()
                }).ToList();

                // Generate export file
                ExportResultsDto exportResult;
                if (format.Equals("xlsx", StringComparison.OrdinalIgnoreCase))
                {
                    exportResult = await excelExportService.GenerateExcelExportAsync(
                        requirementDtos,
                        userStoryDtos,
                        projectId,
                        "xlsx"
                    );
                }
                else
                {
                    exportResult = await excelExportService.GenerateCsvExportAsync(
                        requirementDtos,
                        userStoryDtos,
                        projectId
                    );
                }

                // Record export event (audit trail)
                _logger.LogInformation("Export generated for project {ProjectId}, run {RunId}, format {Format} by user {UserId}", 
                    projectId, runId, format, userId);

                return Response<ExportResultsDto>.Success(
                    exportResult,
                    "Filtered dashboard export generated successfully",
                    200
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating export for project {ProjectId}, run {RunId}", projectId, runId);

                // On error, call AI Cancel endpoint to ensure cancellation from AI side
                try
                {
                    var jobId = runId.ToString();
                    await aiClient.CancelJobAsync(jobId);
                    _logger.LogInformation("AI job {JobId} cancelled due to export error", jobId);
                }
                catch (Exception cancelEx)
                {
                    _logger.LogError(cancelEx, "Failed to cancel AI job {JobId} after export error", runId);
                }

                return Response<ExportResultsDto>.Failure(null, "Failed to generate export", 500);
            }
        }
    }
}
