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

        public async Task<Response<AnalysisRunDto>> StartRunAsync(Guid projectId, StartRunRequest request)
        {
            //check first if projectId Exists
            var project = await dbContext.Projects.FindAsync(projectId);
            if (project == null)
            {
                return Response<AnalysisRunDto>.Failure("Project not found", 404);
            }
            if (request.DocumentIds != null) {
                //check if all documentIds exist and belong to the project
                var documents = await dbContext.Documents
                    .Where(d => request.DocumentIds.Contains(d.Id) && d.ProjectId == projectId)
                    .ToListAsync();
                if (documents.Count != request.DocumentIds.Count)
                {
                    return Response<AnalysisRunDto>.Failure("One or more documents not found or do not belong to the project", 404);
                }
            }

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
                        Progress = activeRun.Progress,
                        CurrentNode = activeRun.CurrentNode,
                        //CurrentNodeLabel = activeRun.CurrentNodeLabel, //want to know Different Nodes and their labels, so later
                        //job_id will be added later "Inicates job id of the current agent"
                        ErrorMessage = activeRun.ErrorMessage,
                        DocumentIds = request.DocumentIds ?? new List<Guid>(),
                        MeetingId = request.MeetingId,
                        CreatedAt = activeRun.CreatedAt,
                        UpdatedAt = activeRun.UpdatedAt,
                        StartedAt = activeRun.StartedAt,
                        CompletedAt = activeRun.CompletedAt
                    },
                    "An active analysis run already exists for this project",
                    200
                );
            }

            if (request.DocumentIds == null || !request.DocumentIds.Any())
            {
                request.DocumentIds = await dbContext.Documents
                    .Where(d => d.ProjectId == projectId)
                    .Select(d => d.Id)
                    .ToListAsync();
                if (request.DocumentIds == null || !request.DocumentIds.Any())
                {
                    return Response<AnalysisRunDto>.Failure(
                        "This project does not contain any documents to process",
                        400
                    );
                }
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
                    await _worker.ProcessRun(run.Id, projectId, request.DocumentIds);
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
                    DocumentIds = request.DocumentIds ?? new List<Guid>(),
                    MeetingId = request.MeetingId,
                    CreatedAt = run.CreatedAt
                },
                "Analysis run started successfully",
                200
            );
        }


        public async Task<Response<AnalysisRunDto>> GetRunAsync(Guid projectId, Guid runId)
        {
            if(runId == Guid.Empty)
                return Response<AnalysisRunDto?>.Failure(new(), "Invalid run ID.", 400);
            var analysisRun = await dbContext.AnalysisRuns.FirstOrDefaultAsync(r => r.Id == runId);
            if (analysisRun == null)
                return Response<AnalysisRunDto?>.Failure(new(), "Run not found.", 404);
       
            var project = await dbContext.Projects.FindAsync(projectId);

            if (project == null)
                return Response<AnalysisRunDto?>.Failure(new(), "Project not found.", 404);
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
                     //CurrentNodeLabel = activeRun.CurrentNodeLabel, //want to know Different Nodes and their labels, so later
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

        public async Task<Response<ResultDto?>> GetResultAsync(Guid projectId, Guid? runId)
        {
            var project = await dbContext.Projects.FindAsync(projectId);
            if (project == null)
                return Response<ResultDto?>.Failure(new(), "Project not found.", 404);

            AnalysisRun? run;

            if (runId.HasValue)
            {
                run = await dbContext.AnalysisRuns
                    .FirstOrDefaultAsync(r => r.Id == runId.Value);
            }
            else
            {
                // Get latest COMPLETED run
                run = await dbContext.AnalysisRuns
                    .Where(r => r.ProjectId == projectId && r.Status == AnalysisRunStatus.COMPLETED)
                    .OrderByDescending(r => r.CompletedAt)
                    .FirstOrDefaultAsync();
            }

            if (run == null)
                return Response<ResultDto?>.Failure(new(), "Run not found.", 404);

            if (run.ProjectId != projectId)
                return Response<ResultDto?>.Failure(new(), "Run does not belong to the specified project.", 400);

            if (run.Status == AnalysisRunStatus.QUEUED ||
                run.Status == AnalysisRunStatus.PROCESSING)
            {
                return Response<ResultDto?>.Success(
                    new(),
                    "Result is not ready yet."
                );
            }

            if (run.Status == AnalysisRunStatus.FAILED)
            {
                return Response<ResultDto?>.Failure(
                    new(),
                    run.ErrorMessage ?? "Analysis failed.",
                    200
                );
            }

            var result = await dbContext.AnalysisResults
                .FirstOrDefaultAsync(r => r.AnalysisRunId == run.Id);

            if (result == null)
            {
                return Response<ResultDto?>.Failure(
                    new(),
                    "Run completed but result is missing.",
                    500
                );
            }

            ResultDto aiData;

            try
            {
                aiData = JsonSerializer.Deserialize<ResultDto>(result.RawJson);
            }
            catch
            {
                return Response<ResultDto?>.Failure(
                    new(),
                    "Invalid AI JSON format",
                    500
                );
            }

            return Response<ResultDto?>.Success(
                aiData,
                "Results retrieved successfully"
            );
        }
    }
}
