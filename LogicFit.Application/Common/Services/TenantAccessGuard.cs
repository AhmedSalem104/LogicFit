using System.Text.Json;
using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Authorization;
using LogicFit.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace LogicFit.Application.Common.Services;

/// <summary>
/// Default <see cref="ITenantAccessGuard"/>. Reads the gym's status + suspension reason and its latest
/// subscription status, and caches the aggregate in <see cref="IDistributedCache"/> for a short window
/// (in-memory today; swap the registration to Redis for multi-instance scale — no code change here).
/// </summary>
public class TenantAccessGuard : ITenantAccessGuard
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);

    private readonly IApplicationDbContext _context;
    private readonly IDistributedCache _cache;
    private readonly IDateTimeService _dateTimeService;

    public TenantAccessGuard(
        IApplicationDbContext context,
        IDistributedCache cache,
        IDateTimeService dateTimeService)
    {
        _context = context;
        _cache = cache;
        _dateTimeService = dateTimeService;
    }

    public async Task<TenantAccessState> GetStateAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        // Platform users (sentinel tenant) are never gated.
        if (tenantId == PlatformConstants.PlatformTenantId)
            return new TenantAccessState(true, TenantStatus.Active, TenantSubscriptionStatus.Active, null);

        var key = $"tenant-access:{tenantId}";

        var cached = await _cache.GetStringAsync(key, cancellationToken);
        if (cached is not null)
        {
            var fromCache = JsonSerializer.Deserialize<TenantAccessState>(cached);
            if (fromCache is not null)
                return fromCache;
        }

        var state = await LoadAsync(tenantId, cancellationToken);

        await _cache.SetStringAsync(
            key,
            JsonSerializer.Serialize(state),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheTtl },
            cancellationToken);

        return state;
    }

    private async Task<TenantAccessState> LoadAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var tenant = await _context.Tenants
            .IgnoreQueryFilters()
            .Where(t => t.Id == tenantId && !t.IsDeleted)
            .Select(t => new { t.Status, t.SuspensionReason })
            .FirstOrDefaultAsync(cancellationToken);

        if (tenant is null)
            return new TenantAccessState(false, TenantStatus.Deleted, null, null);

        var now = _dateTimeService.UtcNow;

        // Latest subscription (any status), by furthest end date — mirrors GetActiveSubscriptionAsync ordering.
        var sub = await _context.TenantSubscriptions
            .Where(s => s.TenantId == tenantId)
            .OrderByDescending(s => s.EndDate)
            .Select(s => new { s.Status, s.EndDate })
            .FirstOrDefaultAsync(cancellationToken);

        TenantSubscriptionStatus? subStatus = sub?.Status;

        // An "Active" subscription whose term has elapsed is effectively expired (the lifecycle job may
        // not have run yet) — treat it as Expired so access reflects reality immediately.
        if (sub is not null
            && sub.Status == TenantSubscriptionStatus.Active
            && sub.EndDate.HasValue
            && sub.EndDate.Value < now)
        {
            subStatus = TenantSubscriptionStatus.Expired;
        }

        return new TenantAccessState(true, tenant.Status, subStatus, tenant.SuspensionReason);
    }
}
