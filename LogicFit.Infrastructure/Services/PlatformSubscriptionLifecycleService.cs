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

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PlatformSubscriptionLifecycleService> _logger;
    private readonly TimeSpan _period = TimeSpan.FromHours(24);

    public PlatformSubscriptionLifecycleService(
        IServiceScopeFactory scopeFactory,
        ILogger<PlatformSubscriptionLifecycleService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PlatformSubscriptionLifecycleService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var usageCalculator = scope.ServiceProvider.GetRequiredService<ITenantUsageCalculator>();
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

                await TransitionSubscriptionsAsync(context, notificationService, stoppingToken);
                await SendExpiryRemindersAsync(context, notificationService, stoppingToken);
                await ExpireStalePaymentRequestsAsync(context, stoppingToken);
                await RecalculateUsageAsync(context, usageCalculator, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in PlatformSubscriptionLifecycleService");
            }

            await Task.Delay(_period, stoppingToken);
        }
    }

    private async Task TransitionSubscriptionsAsync(
        ApplicationDbContext context,
        INotificationService notificationService,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var subscriptions = await context.TenantSubscriptions
            .IgnoreQueryFilters()
            .Where(s => !s.IsDeleted &&
                        (s.Status == TenantSubscriptionStatus.Trial ||
                         s.Status == TenantSubscriptionStatus.Active ||
                         s.Status == TenantSubscriptionStatus.PastDue))
            .ToListAsync(cancellationToken);

        if (subscriptions.Count == 0) return;

        var tenantIds = subscriptions.Select(s => s.TenantId).Distinct().ToList();
        var tenants = await context.Tenants
            .IgnoreQueryFilters()
            .Where(t => tenantIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, cancellationToken);

        var pastDue = 0;
        var suspended = 0;

        foreach (var sub in subscriptions)
        {
            tenants.TryGetValue(sub.TenantId, out var tenant);

            // Trial or Active reaching its end date → PastDue (grace begins).
            var reachedEnd =
                (sub.Status == TenantSubscriptionStatus.Trial && sub.TrialEndsAt.HasValue && sub.TrialEndsAt < now) ||
                (sub.Status == TenantSubscriptionStatus.Active && sub.EndDate.HasValue && sub.EndDate < now);

            if (reachedEnd)
            {
                sub.Status = TenantSubscriptionStatus.PastDue;
                if (tenant != null) tenant.Status = TenantStatus.PastDue;
                pastDue++;
                continue;
            }

            // PastDue past the grace period → Expired + tenant Suspended.
            if (sub.Status == TenantSubscriptionStatus.PastDue)
            {
                var graceRef = sub.EndDate ?? sub.TrialEndsAt;
                if (graceRef.HasValue && graceRef < now.AddDays(-GraceDays))
                {
                    sub.Status = TenantSubscriptionStatus.Expired;
                    sub.SuspendedAt = now;
                    if (tenant != null)
                    {
                        tenant.Status = TenantStatus.Suspended;
                        tenant.SuspensionReason = SuspensionReason.NonPayment;
                    }
                    suspended++;
                    await notificationService.NotifyTenantOwnerAsync(
                        sub.TenantId, NotificationTemplates.TenantSuspended, null, cancellationToken);
                }
            }
        }

        if (pastDue > 0 || suspended > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Lifecycle: {PastDue} moved to PastDue, {Suspended} suspended", pastDue, suspended);
        }
    }

    private async Task SendExpiryRemindersAsync(
        ApplicationDbContext context,
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

    private async Task ExpireStalePaymentRequestsAsync(ApplicationDbContext context, CancellationToken cancellationToken)
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
        ApplicationDbContext context,
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
