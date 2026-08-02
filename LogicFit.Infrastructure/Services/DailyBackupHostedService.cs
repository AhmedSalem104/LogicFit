using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using LogicFit.Application.Common.Interfaces;

namespace LogicFit.Infrastructure.Services;

public sealed class DailyBackupHostedService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<DailyBackupHostedService> logger,
    TimeProvider timeProvider) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var runAt = TimeSpan.TryParse(configuration["Backup:RunAtUtc"], out var configured)
            ? configured : new TimeSpan(2, 0, 0);

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = timeProvider.GetUtcNow();
            var next = now.UtcDateTime.Date.Add(runAt);
            if (next <= now.UtcDateTime) next = next.AddDays(1);
            await Task.Delay(next - now.UtcDateTime, stoppingToken);

            try
            {
                using var scope = scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IBackupService>();
                var result = await service.CreateBatchAsync(
                    new BackupBatchRequest(
                        LogicFit.Domain.Enums.BackupScope.FullSystem,
                        IdempotencyKey: $"daily:{timeProvider.GetUtcNow():yyyyMMdd}"),
                    stoppingToken);
                logger.LogInformation("Daily database backup batch completed: {BatchId} ({Status})", result.Id, result.Status);

                try
                {
                    var media = scope.ServiceProvider.GetRequiredService<IMediaBackupService>();
                    var mediaResult = await media.CreateAsync(stoppingToken);
                    logger.LogInformation("Daily media backup completed: {FileName} ({SizeBytes} bytes)", mediaResult.FileName, mediaResult.SizeBytes);
                }
                catch (Exception mediaException) when (mediaException is not OperationCanceledException || !stoppingToken.IsCancellationRequested)
                {
                    logger.LogError(mediaException, "Database backup succeeded but the media backup failed");
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception ex) { logger.LogError(ex, "Daily database backup failed"); }
        }
    }
}
