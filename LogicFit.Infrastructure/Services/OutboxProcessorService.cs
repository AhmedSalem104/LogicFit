using System.Text.Json;
using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Common.Notifications;
using LogicFit.Domain.Entities;
using LogicFit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LogicFit.Infrastructure.Services;

public sealed class OutboxProcessorService(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxProcessorService> logger,
    IDistributedLockProvider distributedLockProvider) : BackgroundService
{
    private const string LockResource = "LogicFit:Background:OutboxProcessor";
    private static readonly TimeSpan Period = TimeSpan.FromMinutes(1);
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await ProcessAsync(stoppingToken); }
            catch (Exception ex) { logger.LogError(ex, "Outbox processor failed"); }
            await Task.Delay(Period, stoppingToken);
        }
    }

    private async Task ProcessAsync(CancellationToken cancellationToken)
    {
        var lease = await distributedLockProvider.TryAcquireAsync(LockResource, cancellationToken);
        if (lease is null)
        {
            logger.LogDebug("Skipping Outbox pass because another instance owns the lock.");
            return;
        }

        await using (lease)
        {
            using var scope = scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
            var notifier = scope.ServiceProvider.GetRequiredService<INotificationService>();
            var messages = await context.OutboxMessages
                .Where(x => x.ProcessedAtUtc == null && x.AttemptCount < 5)
                .OrderBy(x => x.OccurredAtUtc)
                .ThenBy(x => x.Id)
                .Take(20)
                .ToListAsync(cancellationToken);
            if (messages.Count == 0)
                return;

            var execution = new JobExecutionLog
            {
                JobName = nameof(OutboxProcessorService),
                Status = "Running",
                StartedAtUtc = DateTime.UtcNow,
                AttemptCount = 1,
                Metadata = JsonSerializer.Serialize(new { selected = messages.Count })
            };
            context.JobExecutionLogs.Add(execution);
            await context.SaveChangesAsync(cancellationToken);

            var failed = 0;
            try
            {
                foreach (var message in messages)
                {
                    try
                    {
                        message.AttemptCount++;
                        if (message.Type == "tenant.subscription.expired")
                        {
                            using var json = JsonDocument.Parse(message.Payload);
                            var tenantId = json.RootElement.GetProperty("tenantId").GetGuid();
                            await notifier.NotifyTenantOwnerAsync(tenantId, NotificationTemplates.TenantSuspended, null, cancellationToken);
                        }

                        message.ProcessedAtUtc = DateTime.UtcNow;
                        message.FailedAtUtc = null;
                        message.LastError = null;
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception exception)
                    {
                        failed++;
                        message.FailedAtUtc = DateTime.UtcNow;
                        message.LastError = exception.Message;
                        logger.LogError(exception, "Outbox message {MessageId} failed", message.Id);
                    }
                }

                execution.Status = failed == 0 ? "Completed" : "CompletedWithFailures";
                execution.CompletedAtUtc = DateTime.UtcNow;
                execution.Metadata = JsonSerializer.Serialize(new
                {
                    selected = messages.Count,
                    processed = messages.Count - failed,
                    failed
                });
                await context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                execution.Status = "Failed";
                execution.CompletedAtUtc = DateTime.UtcNow;
                execution.Error = exception.Message;
                try
                {
                    await context.SaveChangesAsync(CancellationToken.None);
                }
                catch (Exception logException)
                {
                    logger.LogError(logException, "Unable to record failed Outbox execution");
                }

                throw;
            }
        }
    }
}
