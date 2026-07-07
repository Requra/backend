using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Requra.Application.DTOs.AI;
using Requra.Application.DTOs.Document;
using Requra.Application.Interfaces.IAIService;
using Requra.Application.Interfaces.IAnalysisRunWorker;
using Requra.Application.Interfaces.IDocumentService;
using Requra.Application.Interfaces.IFileDownloader;
using Requra.Domain.Entities;
using Requra.Domain.Enums;
using Requra.Infrastructure.Data;
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

            try
            {
                var text = await _documentService.GetCombinedText(projectId, documentIds);

                var request = new ProcessJsonRequest
                {
                    JobId = runId.ToString(),
                    SourceType = "multi_document",
                    Content = text,
                    Metadata = new MetadataDto //optional
                    {
                        ProjectId = projectId.ToString(),
                        AnalysisRunId = runId.ToString()
                    }
                };


                var AIStartJobId = await _aiClient.ProcessAsync(request);

                run.UpdateAnalysis(
                    AnalysisRunStatus.QUEUED,
                    0,
                    run.CurrentNode, //need to know first node 
                    run.ErrorMessage,
                    DateTime.UtcNow
                );
                await db.SaveChangesAsync();

                await PollUntilFinished(runId, AIStartJobId);
            }
            catch (Exception ex)
            {
                run.UpdateAnalysis(
                    AnalysisRunStatus.FAILED,
                    0,
                    null,
                    ex.Message,
                    run.StartedAt,
                    DateTime.UtcNow
                );
                await db.SaveChangesAsync();
            }
        }
        private async Task PollUntilFinished(Guid runId, string jobId)
        {
            while (true)
            {
                await Task.Delay(3000);

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<RequraDbContext>();
                var aiClient = scope.ServiceProvider.GetRequiredService<IAIClient>();

                var statusResponse = await aiClient.GetStatusAsync(jobId);

                var run = await db.AnalysisRuns.FindAsync(runId);
                if (run == null) return;

                //run.Status = MapStatus(statusResponse.Status);
                //run.Progress = statusResponse.ProgressPct;

                run.UpdateAnalysis(
                    MapStatus(statusResponse.Status),
                    statusResponse.ProgressPct,
                    statusResponse.CurrentNode,
                    statusResponse.Error,
                    run.StartedAt,
                    run.CompletedAt
                );

                if (statusResponse.Status == "COMPLETED")
                {
                    db.AnalysisResults.Add(new AnalysisResult
                    {
                        AnalysisRunId = runId,
                        RawJson = JsonSerializer.Serialize(statusResponse.Result),
                        CreatedAt = DateTime.UtcNow
                    });
                    run.UpdateAnalysis(
                        AnalysisRunStatus.COMPLETED,
                        statusResponse.ProgressPct,
                        statusResponse.CurrentNode,
                        null,
                        run.StartedAt,
                        DateTime.UtcNow
                    );
            

                    await db.SaveChangesAsync();
                    break;
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
                    break;
                }
                await db.SaveChangesAsync();

            }
        }
        private AnalysisRunStatus MapStatus(string status)
        {
            return status switch
            {
                "QUEUED" => AnalysisRunStatus.QUEUED,
                "PROCESSING" => AnalysisRunStatus.PROCESSING,
                "COMPLETED" => AnalysisRunStatus.COMPLETED,
                "FAILED" => AnalysisRunStatus.FAILED,
            };
        }
    }
}
