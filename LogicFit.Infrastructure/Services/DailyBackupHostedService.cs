using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using LogicFit.Application.Common.Interfaces;

namespace LogicFit.Infrastructure.Services;

public sealed class DailyBackupHostedService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<DailyBackupHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var runAt = TimeSpan.TryParse(configuration["Backup:RunAtUtc"], out var configured)
            ? configured : new TimeSpan(2, 0, 0);

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            var next = now.Date.Add(runAt);
            if (next <= now) next = next.AddDays(1);
            await Task.Delay(next - now, stoppingToken);

            try
            {
                using var scope = scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IBackupService>();
                var result = await service.CreateAsync(stoppingToken);
                logger.LogInformation("Daily database backup completed: {FileName} ({SizeBytes} bytes)", result.FileName, result.SizeBytes);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception ex) { logger.LogError(ex, "Daily database backup failed"); }
        }
    }
}
