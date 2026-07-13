using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Requra.Domain.Enums;
using Requra.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Requra.Infrastructure.Services.StartupRecoveryService
{
    public class StartupRecoveryService : IHostedService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public StartupRecoveryService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RequraDbContext>();

            var stuckRuns = await db.AnalysisRuns
                .Where(r => r.Status == AnalysisRunStatus.PROCESSING || r.Status == AnalysisRunStatus.QUEUED)
                .ToListAsync();

            foreach (var run in stuckRuns)
            {
                run.Status = AnalysisRunStatus.FAILED;
                run.ErrorMessage = "Interrupted due to system shutdown";
            }

            await db.SaveChangesAsync();
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
