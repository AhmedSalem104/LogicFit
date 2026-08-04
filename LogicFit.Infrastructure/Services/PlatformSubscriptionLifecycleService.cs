using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Common.Notifications;
using LogicFit.Domain.Authorization;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LogicFit.Infrastructure.Services;

/// <summary>
/// Daily platform-subscription lifecycle: trials/active → past-due at expiry, past-due → suspended
/// after a grace period, stale pending payment requests expire, and the TenantUsage cache is refreshed.
/// (Reminder + invoice-generation notifications are wired in Phase 8.)
/// </summary>
public class PlatformSubscriptionLifecycleService : BackgroundService
{
    private const int GraceDays = 3;
    private const int PaymentRequestExpiryDays = 14;
    private const int ReminderDaysBefore = 7;
    private const string LockResource = "LogicFit:Background:PlatformSubscriptionLifecycle";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PlatformSubscriptionLifecycleService> _logger;
    private readonly IDistributedLockProvider _distributedLockProvider;
    private readonly TimeSpan _period = TimeSpan.FromHours(24);

    public PlatformSubscriptionLifecycleService(
        IServiceScopeFactory scopeFactory,
        ILogger<PlatformSubscriptionLifecycleService> logger,
        IDistributedLockProvider distributedLockProvider)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _distributedLockProvider = distributedLockProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PlatformSubscriptionLifecycleService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                await RecordFailureAsync(ex, stoppingToken);
            }

            await Task.Delay(_period, stoppingToken);
        }
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        var lease = await _distributedLockProvider.TryAcquireAsync(LockResource, cancellationToken);
        if (lease is null)
        {
            _logger.LogDebug("Skipping platform subscription lifecycle pass because another instance owns the lock.");
            return;
        }

        await using (lease)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
            var usageCalculator = scope.ServiceProvider.GetRequiredService<ITenantUsageCalculator>();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
            var execution = new JobExecutionLog
            {
                JobName = nameof(PlatformSubscriptionLifecycleService),
                Status = "Running",
                StartedAtUtc = DateTime.UtcNow,
                AttemptCount = 1
            };
            context.JobExecutionLogs.Add(execution);
            await context.SaveChangesAsync(cancellationToken);

            await TransitionSubscriptionsAsync(context, notificationService, cancellationToken);
            await SendExpiryRemindersAsync(context, notificationService, cancellationToken);
            await ExpireStalePaymentRequestsAsync(context, cancellationToken);
            await RecalculateUsageAsync(context, usageCalculator, cancellationToken);
            execution.Status = "Completed";
            execution.CompletedAtUtc = DateTime.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task RecordFailureAsync(Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Error in PlatformSubscriptionLifecycleService");
        try
        {
            using var failureScope = _scopeFactory.CreateScope();
            var failureContext = failureScope.ServiceProvider.GetRequiredService<PlatformDbContext>();
            failureContext.JobExecutionLogs.Add(new JobExecutionLog
            {
                JobName = nameof(PlatformSubscriptionLifecycleService),
                Status = "Failed",
                StartedAtUtc = DateTime.UtcNow,
                CompletedAtUtc = DateTime.UtcNow,
                AttemptCount = 1,
                Error = exception.Message
            });
            await failureContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception logException)
        {
            _logger.LogError(logException, "Unable to record failed platform job execution");
        }
    }

    private async Task TransitionSubscriptionsAsync(
        PlatformDbContext context,
        INotificationService notificationService,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var subscriptions = await context.TenantSubscriptions
            .IgnoreQueryFilters()
            .Where(s => !s.IsDeleted &&
                        (s.Status == TenantSubscriptionStatus.Trial ||
                         s.Status == TenantSubscriptionStatus.Active ||
                         s.Status == TenantSubscriptionStatus.PastDue ||
                         s.Status == TenantSubscriptionStatus.Cancelled))
            .ToListAsync(cancellationToken);

        if (subscriptions.Count == 0) return;

        var tenantIds = subscriptions.Select(s => s.TenantId).Distinct().ToList();
        var tenants = await context.Tenants
            .IgnoreQueryFilters()
            .Where(t => tenantIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, cancellationToken);

        var pastDue = 0;
        var expired = 0;

        foreach (var sub in subscriptions)
        {
            tenants.TryGetValue(sub.TenantId, out var tenant);

            // Trial or Active reaching its end date → PastDue (grace begins).
            if (sub.Status == TenantSubscriptionStatus.Trial && sub.TrialEndsAt.HasValue && sub.TrialEndsAt < now)
            {
                sub.Status = TenantSubscriptionStatus.Expired;
                if (tenant != null && tenant.Status == TenantStatus.Trial) tenant.Status = TenantStatus.Active;
                await AddOutboxIfMissingAsync(context, new OutboxMessage
                {
                    Type = "tenant.subscription.expired",
                    Payload = $"{{\"tenantId\":\"{sub.TenantId}\",\"subscriptionId\":\"{sub.Id}\"}}",
                    OccurredAtUtc = now,
                    IdempotencyKey = $"subscription:{sub.Id}:expired:{now:yyyyMMdd}"
                }, cancellationToken);
                expired++;
                continue;
            }

            var reachedEnd = sub.Status == TenantSubscriptionStatus.Active
                && sub.EndDate.HasValue
                && sub.EndDate < now;

            if (reachedEnd)
            {
                sub.Status = TenantSubscriptionStatus.PastDue;
                if (tenant != null) tenant.Status = TenantStatus.PastDue;
                await AddOutboxIfMissingAsync(context, new OutboxMessage
                {
                    Type = "tenant.subscription.past_due",
                    Payload = $"{{\"tenantId\":\"{sub.TenantId}\",\"subscriptionId\":\"{sub.Id}\"}}",
                    OccurredAtUtc = now,
                    IdempotencyKey = $"subscription:{sub.Id}:past-due:{now:yyyyMMdd}"
                }, cancellationToken);
                pastDue++;
                continue;
            }

            if (sub.Status == TenantSubscriptionStatus.Cancelled && sub.EndDate.HasValue && sub.EndDate < now)
            {
                sub.Status = TenantSubscriptionStatus.Expired;
                if (tenant != null && tenant.Status == TenantStatus.Cancelled) tenant.Status = TenantStatus.Active;
                await AddOutboxIfMissingAsync(context, new OutboxMessage
                {
                    Type = "tenant.subscription.expired",
                    Payload = $"{{\"tenantId\":\"{sub.TenantId}\",\"subscriptionId\":\"{sub.Id}\"}}",
                    OccurredAtUtc = now,
                    IdempotencyKey = $"subscription:{sub.Id}:expired:{now:yyyyMMdd}"
                }, cancellationToken);
                expired++;
                continue;
            }

            // PastDue past the grace period → Expired + tenant Suspended.
            if (sub.Status == TenantSubscriptionStatus.PastDue)
            {
                var graceRef = sub.EndDate ?? sub.TrialEndsAt;
                if (graceRef.HasValue && graceRef < now.AddDays(-GraceDays))
                {
                    sub.Status = TenantSubscriptionStatus.Expired;
                    if (tenant != null && tenant.Status == TenantStatus.PastDue)
                        tenant.Status = TenantStatus.Active;
                    await AddOutboxIfMissingAsync(context, new OutboxMessage
                    {
                        Type = "tenant.subscription.expired",
                        Payload = $"{{\"tenantId\":\"{sub.TenantId}\",\"subscriptionId\":\"{sub.Id}\"}}",
                        OccurredAtUtc = now,
                        IdempotencyKey = $"subscription:{sub.Id}:expired:{now:yyyyMMdd}"
                    }, cancellationToken);
                    expired++;
                }
            }
        }

        if (pastDue > 0 || expired > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Lifecycle: {PastDue} moved to PastDue, {Expired} moved to Expired", pastDue, expired);
        }
    }

    private static async Task AddOutboxIfMissingAsync(
        PlatformDbContext context,
        OutboxMessage message,
        CancellationToken cancellationToken)
    {
        if (context.OutboxMessages.Local.Any(existing => existing.IdempotencyKey == message.IdempotencyKey))
            return;

        if (await context.OutboxMessages.AnyAsync(
                existing => existing.IdempotencyKey == message.IdempotencyKey,
                cancellationToken))
            return;

        context.OutboxMessages.Add(message);
    }

    private async Task SendExpiryRemindersAsync(
        PlatformDbContext context,
        INotificationService notificationService,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var window = now.AddDays(ReminderDaysBefore);

        var expiring = await context.TenantSubscriptions
            .IgnoreQueryFilters()
            .Where(s => !s.IsDeleted &&
                        s.Status == TenantSubscriptionStatus.Active &&
                        s.EndDate != null && s.EndDate > now && s.EndDate <= window &&
                        s.ReminderSentAt == null)
            .ToListAsync(cancellationToken);

        foreach (var sub in expiring)
        {
            var days = Math.Max(1, (int)Math.Ceiling((sub.EndDate!.Value - now).TotalDays));
            await notificationService.NotifyTenantOwnerAsync(
                sub.TenantId,
                NotificationTemplates.SubscriptionExpiringSoon,
                new Dictionary<string, string> { ["days"] = days.ToString() },
                cancellationToken);
            sub.ReminderSentAt = now;
        }

        if (expiring.Count > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Sent {Count} expiry reminders", expiring.Count);
        }
    }

    private async Task ExpireStalePaymentRequestsAsync(PlatformDbContext context, CancellationToken cancellationToken)
    {
        var cutoff = DateTime.UtcNow.AddDays(-PaymentRequestExpiryDays);

        var stale = await context.PaymentRequests
            .IgnoreQueryFilters()
            .Where(p => p.Status == PaymentRequestStatus.Pending && !p.IsDeleted && p.CreatedAt < cutoff)
            .ToListAsync(cancellationToken);

        foreach (var pr in stale)
        {
            pr.Status = PaymentRequestStatus.Expired;
        }

        if (stale.Count > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Expired {Count} stale payment requests", stale.Count);
        }
    }

    private async Task RecalculateUsageAsync(
        PlatformDbContext context,
        ITenantUsageCalculator usageCalculator,
        CancellationToken cancellationToken)
    {
        var tenantIds = await context.Tenants
            .IgnoreQueryFilters()
            .Where(t => t.Id != PlatformConstants.PlatformTenantId && !t.IsDeleted)
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);

        if (tenantIds.Count == 0) return;

        var existing = await context.TenantUsages
            .ToDictionaryAsync(u => u.TenantId, cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var tenantId in tenantIds)
        {
            var snapshot = await usageCalculator.CalculateAsync(tenantId, cancellationToken);

            if (!existing.TryGetValue(tenantId, out var usage))
            {
                usage = new TenantUsage { TenantId = tenantId };
                context.TenantUsages.Add(usage);
            }

            usage.MembersCount = snapshot.Members;
            usage.CoachesCount = snapshot.Coaches;
            usage.BranchesCount = snapshot.Branches;
            usage.EmployeesCount = snapshot.Employees;
            usage.LastCalculatedAt = now;
        }

        await context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Recalculated usage for {Count} tenants", tenantIds.Count);
    }
}
