using CloudinaryDotNet.Actions;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.EntityFrameworkCore;
using Requra.Application.DTOs.AI;
using Requra.Application.DTOs.Profile;
using Requra.Application.Interfaces.IAIService;
using Requra.Application.Interfaces.IAnalysisRunService;
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
    public class AnalysisRunService(IUnitOfWork unitOfWork,RequraDbContext dbContext, IAIClient aiClient, IDocumentService documentService) : IAnalysisRunService
    {

        public async Task<Response<AnalysisRunDto>> StartRunAsync(Guid projectId, List<Guid> documentIds, Guid? meetingId)
        {
            var run = new AnalysisRun
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Status = AnalysisRunStatus.QUEUED,
                CreatedAt = DateTime.UtcNow
            };

            //await unitOfWork.Repository<AnalysisRun>().AddAsync(run);
            dbContext.AnalysisRuns.Add(run);
            await dbContext.SaveChangesAsync();

            _ = Task.Run(() => ProcessRun(run.Id, projectId, documentIds));

            return Response<AnalysisRunDto>.Success(
                   new AnalysisRunDto
                   {
                       Id = run.Id,
                       ProjectId = run.ProjectId,
                       Status = run.Status.ToString(),
                       DocumentIds = documentIds,
                       MeetingId = meetingId,
                       CreatedAt = run.CreatedAt
                   },
                   "Analysis run started successfully",
                   200
               );
        }

        private async Task ProcessRun(Guid runId, Guid projectId, List<Guid> documentIds)
        {
            var run = await unitOfWork.Repository<AnalysisRun>().GetByIdAsync(runId);
            run.Status = AnalysisRunStatus.PROCESSING;
            run.StartedAt = DateTime.UtcNow;
            dbContext.AnalysisRuns.Update(run);
            await dbContext.SaveChangesAsync();
            try
            {
                var text = await documentService.GetCombinedText(projectId, documentIds);

                var request = new ProcessJsonRequest
                {
                    Job_Id = runId.ToString(),
                    Source_Type = "multi_document",
                    Content = text,
                    Source_Documents = new List<SourceDocumentDto>(),
                    Metadata = new MetadataDto
                    {
                        Project_Id = projectId,
                        Analysis_Run_Id = runId
                    }
                };

                var aiResult = await aiClient.ProcessAsync(request);

                var result = new AnalysisResult
                {
                    AnalysisRunId = runId,
                    RawJson = JsonSerializer.Serialize(aiResult),
                    CreatedAt = DateTime.UtcNow
                };

                //await unitOfWork.Repository<AnalysisResult>().AddAsync(result);
                dbContext.AnalysisResults.Add(result);
                await dbContext.SaveChangesAsync();

                //run.Status = AnalysisRunStatus.COMPLETED;
                //run.CompletedAt = DateTime.UtcNow;
                //dbContext.AnalysisRuns.Update(run);

                await dbContext.AnalysisRuns.Where(r => r.Id == run.Id)
                    .ExecuteUpdateAsync(r => r
                        .SetProperty(r => r.Status, AnalysisRunStatus.COMPLETED)
                        .SetProperty(r => r.CompletedAt, DateTime.UtcNow));
                await dbContext.SaveChangesAsync();



            }
            catch (Exception ex)
            {
                run.Status = AnalysisRunStatus.FAILED;
                run.ErrorMessage = ex.Message;

                dbContext.AnalysisRuns.Update(run);
                await dbContext.SaveChangesAsync();
            }
        }

        public async Task<AnalysisRun> GetRunAsync(Guid runId)
            => await unitOfWork.Repository<AnalysisRun>().GetByIdAsync(runId);

        public async Task<AnalysisResult> GetResultAsync(Guid runId)
            => await unitOfWork.Repository<AnalysisResult>().GetByIdAsync(runId);
    }
}
