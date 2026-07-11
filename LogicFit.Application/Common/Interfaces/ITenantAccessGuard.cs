using LogicFit.Application.Common.Services;

namespace LogicFit.Application.Common.Interfaces;

/// <summary>
/// Aggregates a gym's status + latest subscription status + suspension reason into a single
/// <see cref="TenantAccessState"/>, backed by a short distributed cache so the per-request gate does
/// not hit the database on every call. Consumed by the login gate, the request middleware, and the
/// tenant authorization handler.
/// </summary>
public interface ITenantAccessGuard
{
    Task<TenantAccessState> GetStateAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
