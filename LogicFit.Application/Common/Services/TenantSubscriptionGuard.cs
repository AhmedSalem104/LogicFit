using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Common.Services;

/// <summary>
/// Enforces plan features and usage limits. Tenants with no active platform subscription are
/// grandfathered (not blocked); enforcement kicks in once a tenant is on an active plan. Usage is
/// counted live (authoritative) rather than from the TenantUsage cache to avoid overage races.
/// </summary>
public class TenantSubscriptionGuard : ITenantSubscriptionGuard
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly IDateTimeService _dateTimeService;

    public TenantSubscriptionGuard(
        IApplicationDbContext context,
        ITenantService tenantService,
        IDateTimeService dateTimeService)
    {
        _context = context;
        _tenantService = tenantService;
        _dateTimeService = dateTimeService;
    }

    public async Task EnsureFeatureAsync(string featureCode, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var feature = await _context.Features
            .FirstOrDefaultAsync(f => f.Code == featureCode, cancellationToken);
        if (feature == null)
        {
            return; // Unknown feature code — don't block.
        }

        // A per-tenant override wins over the plan.
        var over = await _context.TenantFeatures
            .FirstOrDefaultAsync(tf => tf.TenantId == tenantId && tf.FeatureId == feature.Id, cancellationToken);
        if (over != null)
        {
            if (!over.IsEnabled)
            {
                throw new SubscriptionLimitException($"The '{featureCode}' feature is disabled for your gym.");
            }
            return;
        }

        var subscription = await GetActiveSubscriptionAsync(tenantId, cancellationToken);
        if (subscription == null)
        {
            if (!feature.IsFree)
                throw new SubscriptionLimitException($"The '{featureCode}' feature requires an active subscription.");
            return;
        }

        var included = await _context.SubscriptionFeatureSnapshots
            .AnyAsync(sf => sf.TenantSubscriptionId == subscription.Id && sf.FeatureKey == featureCode && sf.IsEnabled, cancellationToken);
        if (!included)
            included = await _context.PlanFeatures.AnyAsync(pf => pf.PlanId == subscription.PlanId && pf.FeatureId == feature.Id, cancellationToken);
        if (!included)
        {
            throw new SubscriptionLimitException($"The '{featureCode}' feature is not included in your current plan.");
        }
    }

    public async Task EnsureQuotaAsync(string quotaResource, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var subscription = await GetActiveSubscriptionAsync(tenantId, cancellationToken);
        if (subscription == null)
        {
            throw new SubscriptionLimitException("An active subscription is required for quota-managed resources.");
        }

        var plan = await _context.Plans.FirstOrDefaultAsync(p => p.Id == subscription.PlanId, cancellationToken);
        if (plan == null)
        {
            return;
        }

        int? limit = quotaResource switch
        {
            QuotaResources.Members => plan.MaxMembers,
            QuotaResources.Coaches => plan.MaxCoaches,
            QuotaResources.Branches => plan.MaxBranches,
            QuotaResources.Employees => plan.MaxEmployees,
            _ => null
        };

        if (limit == null)
        {
            return; // Unlimited.
        }

        var used = await CountUsageAsync(quotaResource, tenantId, cancellationToken);
        if (used >= limit.Value)
        {
            throw new SubscriptionLimitException(
                $"You have reached your plan limit for {quotaResource} ({limit.Value}). Upgrade your plan to add more.");
        }
    }

    private async Task<TenantSubscription?> GetActiveSubscriptionAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var now = _dateTimeService.UtcNow;
        return await _context.TenantSubscriptions
            .Where(s => s.TenantId == tenantId &&
                        (s.Status == TenantSubscriptionStatus.Active || s.Status == TenantSubscriptionStatus.GracePeriod) &&
                        (s.EndDate == null || s.EndDate > now))
            .OrderByDescending(s => s.EndDate)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<int> CountUsageAsync(string quotaResource, Guid tenantId, CancellationToken cancellationToken)
    {
        return quotaResource switch
        {
            QuotaResources.Members => await _context.Users.IgnoreQueryFilters()
                .CountAsync(u => u.TenantId == tenantId && u.Role == UserRole.Client && !u.IsDeleted, cancellationToken),
            QuotaResources.Coaches => await _context.Users.IgnoreQueryFilters()
                .CountAsync(u => u.TenantId == tenantId && u.Role == UserRole.Coach && !u.IsDeleted, cancellationToken),
            QuotaResources.Branches => await _context.Branches.IgnoreQueryFilters()
                .CountAsync(b => b.TenantId == tenantId && !b.IsDeleted, cancellationToken),
            QuotaResources.Employees => await _context.EmployeeProfiles.IgnoreQueryFilters()
                .CountAsync(e => e.TenantId == tenantId && !e.IsDeleted, cancellationToken),
            _ => 0
        };
    }
}
