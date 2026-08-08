using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Enums;
using LogicFit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LogicFit.Infrastructure.Services;

public class SubscriptionLifecycleService : BackgroundService
{
    private const string LockResource = "LogicFit:Background:TenantSubscriptionLifecycle";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SubscriptionLifecycleService> _logger;
    private readonly IDistributedLockProvider _distributedLockProvider;
    private readonly TimeSpan _period = TimeSpan.FromHours(24);

    public SubscriptionLifecycleService(
        IServiceScopeFactory scopeFactory,
        ILogger<SubscriptionLifecycleService> logger,
        IDistributedLockProvider distributedLockProvider)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _distributedLockProvider = distributedLockProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SubscriptionLifecycleService started");

        // Run immediately on startup, then every 24 hours
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessSubscriptions(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SubscriptionLifecycleService");
                await RecordAsync("Failed", ex.Message, stoppingToken);
            }

            await Task.Delay(_period, stoppingToken);
        }
    }

    private async Task ProcessSubscriptions(CancellationToken cancellationToken)
    {
        var lease = await _distributedLockProvider.TryAcquireAsync(LockResource, cancellationToken);
        if (lease is null)
        {
            _logger.LogDebug("Skipping tenant subscription lifecycle pass because another instance owns the lock.");
            return;
        }

        await using (lease)
        {
            await ProcessSubscriptionsUnderLock(cancellationToken);
        }
    }

    private async Task ProcessSubscriptionsUnderLock(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var execution = new LogicFit.Domain.Entities.JobExecutionLog
        {
            JobName = nameof(SubscriptionLifecycleService), Status = "Running", StartedAtUtc = DateTime.UtcNow, AttemptCount = 1
        };
        context.JobExecutionLogs.Add(execution);
        await context.SaveChangesAsync(cancellationToken);

        await ExpireSubscriptions(context, cancellationToken);
        await UnfreezeSubscriptions(context, cancellationToken);
        execution.Status = "Completed";
        execution.CompletedAtUtc = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task RecordAsync(string status, string? error, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.JobExecutionLogs.Add(new LogicFit.Domain.Entities.JobExecutionLog
            {
                JobName = nameof(SubscriptionLifecycleService), Status = status, StartedAtUtc = DateTime.UtcNow,
                CompletedAtUtc = DateTime.UtcNow, AttemptCount = 1, Error = error
            });
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception logEx) { _logger.LogError(logEx, "Unable to record subscription job execution"); }
    }

    private async Task ProcessSubscriptionsUnderLock(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var platform = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        var resolver = scope.ServiceProvider.GetRequiredService<ITenantDatabaseResolver>();
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
                _logger.LogWarning(
                    "Skipping tenant subscription lifecycle for TenantId {TenantId}; no active database mapping is available.",
                    tenantId);
                continue;
            }

            await using var context = TenantRuntimeDbContextFactory.Create(resolution);
            var execution = new LogicFit.Domain.Entities.JobExecutionLog
            {
                JobName = nameof(SubscriptionLifecycleService),
                Status = "Running",
                StartedAtUtc = DateTime.UtcNow,
                AttemptCount = 1
            };
            context.JobExecutionLogs.Add(execution);

            try
            {
                await context.SaveChangesAsync(cancellationToken);
                await ExpireSubscriptions(context, cancellationToken);
                await UnfreezeSubscriptions(context, cancellationToken);
                execution.Status = "Completed";
                execution.CompletedAtUtc = DateTime.UtcNow;
                await context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
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
                    _logger.LogError(logException, "Unable to record tenant subscription job failure for TenantId {TenantId}.", tenantId);
                }

                _logger.LogError(exception, "Tenant subscription lifecycle failed for TenantId {TenantId}.", tenantId);
            }
        }
    }

    private async Task RecordAsync(string status, string? error, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
            context.JobExecutionLogs.Add(new LogicFit.Domain.Entities.JobExecutionLog
            {
                JobName = nameof(SubscriptionLifecycleService), Status = status, StartedAtUtc = DateTime.UtcNow,
                CompletedAtUtc = DateTime.UtcNow, AttemptCount = 1, Error = error
            });
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception logEx) { _logger.LogError(logEx, "Unable to record subscription job execution"); }
    }

    private async Task ExpireSubscriptions(TenantDbContext context, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var expiredSubscriptions = await context.ClientSubscriptions
            .IgnoreQueryFilters()
            .Where(s => s.Status == SubscriptionStatus.Active
                && s.EndDate < now
                && !s.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var subscription in expiredSubscriptions)
        {
            subscription.Status = SubscriptionStatus.Expired;
        }

        if (expiredSubscriptions.Count > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Expired {Count} subscriptions", expiredSubscriptions.Count);
        }
    }

    private async Task UnfreezeSubscriptions(TenantDbContext context, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var expiredFreezes = await context.SubscriptionFreezes
            .IgnoreQueryFilters()
            .Include(f => f.Subscription)
            .Where(f => f.IsActive && f.EndDate <= now && !f.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var freeze in expiredFreezes)
        {
            freeze.IsActive = false;

            if (freeze.Subscription.Status == SubscriptionStatus.Suspended)
            {
                freeze.Subscription.Status = SubscriptionStatus.Active;
            }
        }

        if (expiredFreezes.Count > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Unfroze {Count} subscriptions", expiredFreezes.Count);
        }
    }
}
