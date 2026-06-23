using Microsoft.Extensions.DependencyInjection;
using Requra.Application.DTOs.AI;
using Requra.Application.Interfaces.IAIService;
using Requra.Application.Interfaces.IAnalysisRunWorker;
using Requra.Application.Interfaces.IDocumentService;
using Requra.Domain.Entities;
using Requra.Domain.Enums;
using Requra.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;


namespace Requra.Infrastructure.Workers.AnalysisRunWorker
{
    public class AnalysisRunWorker(IServiceScopeFactory _scopeFactory) : IAnalysisRunWorker
    {
       

        public async Task ProcessRun(Guid runId, Guid projectId, List<Guid> documentIds)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RequraDbContext>();
            var _aiClient = scope.ServiceProvider.GetRequiredService<IAIClient>();
            var _documentService = scope.ServiceProvider.GetRequiredService<IDocumentService>();

            var run = await db.AnalysisRuns.FirstOrDefaultAsync(x => x.Id == runId);

            run.Status = AnalysisRunStatus.PROCESSING;
            run.StartedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            try
            {
                var text = await _documentService.GetCombinedText(projectId, documentIds);
                var request = new ProcessJsonRequest
                {
                    Job_Id = runId,
                    Source_Type = "multi_document",
                    Content = text,
                    Metadata = new MetadataDto
                    {
                        Project_Id = projectId,
                        Analysis_Run_Id = runId
                    }
                };

                var aiResult = await _aiClient.ProcessAsync(request);

                db.AnalysisResults.Add(new AnalysisResult
                {
                    AnalysisRunId = runId,
                    RawJson = JsonSerializer.Serialize(aiResult),
                    CreatedAt = DateTime.UtcNow
                });

                await db.SaveChangesAsync();

                await db.AnalysisRuns
                    .Where(r => r.Id == runId)
                    .ExecuteUpdateAsync(r => r
                        .SetProperty(x => x.Status, AnalysisRunStatus.COMPLETED)
                        .SetProperty(x => x.CompletedAt, DateTime.UtcNow));
            }
            catch (Exception ex)
            {
                run.Status = AnalysisRunStatus.FAILED;
                run.ErrorMessage = ex.Message;
                await db.SaveChangesAsync();
            }
        }
    }
}
