using LogicFit.Application.Common.Interfaces;

namespace LogicFit.Infrastructure.Services;

/// <summary>
/// Development/CI provider backed by operator-registered local SQL Server resources. It shares
/// the same idempotent migration, seed, health-check, and mapping engine as Monster, but never
/// falls back to the Platform database. Local test harnesses register multiple local resources
/// in the resource pool and may safely remove those test databases outside the application.
/// </summary>
public sealed class LocalSqlProvisioningProvider(ManualMonsterProvisioningProvider engine)
    : IDatabaseProvisioningProvider
{
    public string ProviderName => "LocalSql";

    public async Task<DatabaseProvisioningResult> ProvisionAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var result = await engine.ProvisionAsync(tenantId, cancellationToken);
        return result with { Provider = ProviderName };
    }
}
