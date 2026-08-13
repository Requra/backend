using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Requra.Application.Interfaces.IRecordingService;

namespace Requra.Infrastructure.Services.RecordingService
{
    public class RecordingBackgroundJobService(
        IServiceScopeFactory scopeFactory,
        ILogger<RecordingBackgroundJobService> logger) : IRecordingBackgroundJobService
    {
        public Task EnqueueFinalizeRecordingAsync(Guid recordingId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _ = Task.Run(async () =>
            {
                try
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var finalizationService = scope.ServiceProvider
                        .GetRequiredService<IRecordingFinalizationService>();

                    await finalizationService.FinalizeRecordingAsync(
                        recordingId,
                        CancellationToken.None);
                }
                catch (Exception ex)
                {
                    logger.LogError(
                        ex,
                        "Recording finalization failed for recording {RecordingId}.",
                        recordingId);
                }
            });

            return Task.CompletedTask;
        }
    }
}
