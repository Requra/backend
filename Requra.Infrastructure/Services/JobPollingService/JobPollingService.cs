using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Requra.Application.Interfaces.IAIService;
using Requra.Domain.Entities;
using Requra.Domain.Enums;
using Requra.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Requra.Infrastructure.Services.JobPollingService
{
    public class JobPollingService(IServiceScopeFactory _scopeFactory, ILogger<JobPollingService> _logger) : IJobPollingService
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
                        db.AnalysisResults.Add(new AnalysisResult
                        {
                            AnalysisRunId = runId,
                            RawJson = JsonSerializer.Serialize(result),
                            CreatedAt = DateTime.UtcNow
                        });
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
    }
}
