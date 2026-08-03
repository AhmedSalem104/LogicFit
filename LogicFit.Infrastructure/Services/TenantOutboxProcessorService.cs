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

/// <summary>
/// Processes the local Outbox projection in each mapped Tenant DB. Platform outbox messages are
/// handled by OutboxProcessorService; this worker deliberately creates a fresh scope per tenant
/// so the request-scoped tenant context can never be reused for another workspace.
/// </summary>
public sealed class TenantOutboxProcessorService(
    IServiceScopeFactory scopeFactory,
    ILogger<TenantOutboxProcessorService> logger,
    IDistributedLockProvider distributedLockProvider) : BackgroundService
{
    private const string LockResource = "LogicFit:Background:TenantOutboxProcessor";
    private static readonly TimeSpan Period = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Tenant Outbox processor failed.");
            }

            await Task.Delay(Period, stoppingToken);
        }
    }

    private async Task ProcessAsync(CancellationToken cancellationToken)
    {
        var lease = await distributedLockProvider.TryAcquireAsync(LockResource, cancellationToken);
        if (lease is null)
        {
            logger.LogDebug("Skipping tenant Outbox pass because another instance owns the lock.");
            return;
        }

        await using (lease)
        {
            using var platformScope = scopeFactory.CreateScope();
            var platform = platformScope.ServiceProvider.GetRequiredService<PlatformDbContext>();
            var resolver = platformScope.ServiceProvider.GetRequiredService<ITenantDatabaseResolver>();
            var tenantIds = await platform.TenantDatabaseMappings
                .AsNoTracking()
                .Where(mapping => mapping.IsActive)
                .Select(mapping => mapping.TenantId)
                .Distinct()
                .ToListAsync(cancellationToken);

            foreach (var tenantId in tenantIds)
            {
                var resolution = await resolver.ResolveAsync(tenantId, cancellationToken);
                if (resolution is null)
                {
                    logger.LogWarning(
                        "Skipping Tenant Outbox for TenantId {TenantId}; no active database mapping is available.",
                        tenantId);
                    continue;
                }

                await ProcessTenantAsync(tenantId, resolution, cancellationToken);
            }
        }
    }

    private async Task ProcessTenantAsync(
        Guid tenantId,
        TenantDatabaseResolution resolution,
        CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var tenantService = scope.ServiceProvider.GetRequiredService<ITenantService>();
        var requestScope = scope.ServiceProvider.GetRequiredService<TenantDatabaseRequestScope>();
        await tenantService.SetTenantAsync(tenantId);
        requestScope.Set(resolution);

        try
        {
            var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
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
                JobName = nameof(TenantOutboxProcessorService),
                Status = "Running",
                StartedAtUtc = DateTime.UtcNow,
                AttemptCount = 1,
                Metadata = JsonSerializer.Serialize(new { selected = messages.Count })
            };
            context.JobExecutionLogs.Add(execution);
            await context.SaveChangesAsync(cancellationToken);

            var failed = 0;
            foreach (var message in messages)
            {
                try
                {
                    message.AttemptCount++;
                    if (message.Type == "tenant.subscription.expired")
                    {
                        using var json = JsonDocument.Parse(message.Payload);
                        var messageTenantId = json.RootElement.GetProperty("tenantId").GetGuid();
                        if (messageTenantId != tenantId)
                            throw new InvalidOperationException("Tenant Outbox message belongs to another workspace.");

                        await notifier.NotifyTenantOwnerAsync(
                            tenantId,
                            NotificationTemplates.TenantSuspended,
                            null,
                            cancellationToken);
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
                    logger.LogError(exception, "Tenant Outbox message {MessageId} failed for TenantId {TenantId}.", message.Id, tenantId);
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
        finally
        {
            requestScope.Clear();
        }
    }
}
