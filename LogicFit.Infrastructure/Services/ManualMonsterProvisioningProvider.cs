using LogicFit.Application.Common.Interfaces;

namespace LogicFit.Infrastructure.Services;

/// <summary>
/// Monster's current provider only reserves an operator-registered database. Database creation,
/// deletion and native restore are intentionally not attempted by this provider.
/// Migrations/seed/health/mapping assignment are completed by the provisioning saga (#166).
/// </summary>
public sealed class ManualMonsterProvisioningProvider(IDatabaseResourcePool resourcePool) : IDatabaseProvisioningProvider
{
    public string ProviderName => "ManualMonster";

    public async Task<DatabaseProvisioningResult> ProvisionAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var reservation = await resourcePool.ReserveAvailableAsync(tenantId, cancellationToken);
        return reservation is null
            ? new DatabaseProvisioningResult("AwaitingDatabaseCapacity", tenantId, null, ProviderName, null, "DATABASE_CAPACITY_UNAVAILABLE")
            : new DatabaseProvisioningResult("Reserved", tenantId, reservation.ResourceId, reservation.Provider, reservation.DatabaseName);
    }
}
