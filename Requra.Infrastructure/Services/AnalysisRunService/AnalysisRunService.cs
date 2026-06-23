using CloudinaryDotNet.Actions;
using DocumentFormat.OpenXml.Office2010.Word;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Requra.Application.DTOs.AI;
using Requra.Application.Interfaces.IAIService;
using Requra.Application.Interfaces.IAnalysisRunService;
using Requra.Application.Interfaces.IAnalysisRunWorker;
using Requra.Application.Interfaces.IDocumentService;
using Requra.Application.Response;
using Requra.Domain.Entities;
using Requra.Domain.Enums;
using Requra.Infrastructure.Data;
using Requra.Infrastructure.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Requra.Infrastructure.Services.AnalysisRunService
{
    public class AnalysisRunService(ILogger<AnalysisRunService> _logger, IAnalysisRunWorker _worker, IServiceScopeFactory _scopeFactory, RequraDbContext dbContext, IAIClient aiClient, IDocumentService documentService) : IAnalysisRunService
    {

        public async Task<Response<AnalysisRunDto>> StartRunAsync(Guid projectId, List<Guid>? documentIds, Guid? meetingId)
        {
            var activeRun = await dbContext.AnalysisRuns
                .Where(x => x.ProjectId == projectId &&
                       (x.Status == AnalysisRunStatus.QUEUED ||
                        x.Status == AnalysisRunStatus.PROCESSING))
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();

            if (activeRun != null)
            {
                return Response<AnalysisRunDto>.Success(
                    new AnalysisRunDto
                    {
                        Id = activeRun.Id,
                        ProjectId = activeRun.ProjectId,
                        Status = activeRun.Status,
                        DocumentIds = documentIds ?? new List<Guid>(),
                        MeetingId = meetingId,
                        CreatedAt = activeRun.CreatedAt
                    },
                    "An active analysis run already exists for this project",
                    200
                );
            }

            if (documentIds == null || !documentIds.Any())
            {
                documentIds = await dbContext.Documents
                    .Where(d => d.ProjectId == projectId)
                    .Select(d => d.Id)
                    .ToListAsync();
            }

            var run = new AnalysisRun
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Status = AnalysisRunStatus.QUEUED,
                CreatedAt = DateTime.UtcNow
            };

            dbContext.AnalysisRuns.Add(run);
            await dbContext.SaveChangesAsync();

            _ = Task.Run(async () =>
            {
                try
                {
                    await _worker.ProcessRun(run.Id, projectId, documentIds);
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
                    DocumentIds = documentIds,
                    MeetingId = meetingId,
                    CreatedAt = run.CreatedAt
                },
                "Analysis run started successfully",
                200
            );
        }


        public async Task<Response<AnalysisRunStatusDto>> GetRunAsync(Guid runId)
        {
            var analysisRun = await dbContext.AnalysisRuns.FirstOrDefaultAsync(r => r.Id == runId);
            return Response<AnalysisRunStatusDto>.Success(
                  new AnalysisRunStatusDto
                  {
                      Id = analysisRun.Id,
                      ProjectId = analysisRun.ProjectId,
                      Status = analysisRun.Status,
                      //progress should be handeled
                      Progress = analysisRun.Progress,
                      Messsage = analysisRun.Status switch
                      {
                          AnalysisRunStatus.QUEUED =>
                              "Your analysis is waiting in the queue",

                          AnalysisRunStatus.PROCESSING =>
                              "AI is analyzing documents and extracting requirements",

                          AnalysisRunStatus.COMPLETED =>
                              "Analysis completed successfully",

                          AnalysisRunStatus.FAILED =>
                              "Analysis failed. Please check error details",

                          _ => "Unknown status"
                      },
                      ErrorMessage = analysisRun.ErrorMessage,
                      CreatedAt = analysisRun.CreatedAt,
                      StartedAt = analysisRun.StartedAt,
                      CompletedAt = analysisRun.CompletedAt,
                  },
                  "Analysis run status retrieved successfully",
                  200
              );


        }

        public async Task<Response<ResultsDashboardDto?>> GetResultAsync(Guid runId)
        {
            var run = await dbContext.AnalysisRuns
                .FirstOrDefaultAsync(r => r.Id == runId);

            if (run == null)
                return Response<ResultsDashboardDto?>.Failure(new(),"Run not found.", 404);

            if (run.Status == AnalysisRunStatus.QUEUED ||
                run.Status == AnalysisRunStatus.PROCESSING)
            {
                return Response<ResultsDashboardDto?>.Success(
                    new(),
                    "Result is not ready yet."
                );
            }

            if (run.Status == AnalysisRunStatus.FAILED)
            {
                return Response<ResultsDashboardDto?>.Failure(new(),
                    run.ErrorMessage ?? "Analysis failed.",
                    200 // still OK request, but failed business-wise
                );
            }

            var result = await dbContext.AnalysisResults
                .FirstOrDefaultAsync(r => r.AnalysisRunId == runId);

            if (result == null)
            {
                return Response<ResultsDashboardDto?>.Failure(new(),
                    "Run completed but result is missing.",
                    500
                );
            }

            ProcessJsonResponse aiData;

            try
            {
                aiData = JsonSerializer.Deserialize<ProcessJsonResponse>(result.RawJson);
            }
            catch
            {
                return Response<ResultsDashboardDto?>.Failure(new(),
                    "Invalid AI JSON format",
                    500
                );
            }

            var dashboard = new ResultsDashboardDto
            {
                ProjectId = run.ProjectId.ToString(),
                AnalysisRunId = run.Id.ToString(),
                Status = run.Status.ToString(),

                Summary = aiData.Summary,

                Metrics = new MetricsDto
                {
                    TotalRequirements = aiData.Requirements?.Count ?? 0,
                    FunctionalRequirements = aiData.Requirements?.Count(r => r.Type == "Functional") ?? 0,
                    NonFunctionalRequirements = aiData.Requirements?.Count(r => r.Type == "NonFunctional") ?? 0,
                    HighPriorityItems = aiData.Requirements?.Count(r => r.Priority == "High") ?? 0,
                    RisksCount = aiData.Risks?.Count ?? 0,
                    OpenQuestionsCount = aiData.OpenQuestions?.Count ?? 0
                },

                Requirements = aiData.Requirements,
                UserStories = aiData.UserStories,
                Risks = aiData.Risks,
                OpenQuestions = aiData.OpenQuestions,
                ActionItems = aiData.ActionItems,

                GeneratedAt = run.CompletedAt ?? DateTime.UtcNow
            };

            return Response<ResultsDashboardDto?>.Success(
                dashboard,
                "Results retrieved successfully"
            );
        }
    }
}
