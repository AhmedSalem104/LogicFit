using LogicFit.Application.Common.Interfaces;

namespace LogicFit.Infrastructure.Services;

/// <summary>
/// Monster Free is intentionally operator-only for destructive database operations. An explicit
/// provider/capability gate prevents an API request from dropping a production database.
/// </summary>
public sealed class ManualMonsterTenantDatabasePurgeProvider : ITenantDatabasePurgeProvider
{
    public TenantDatabasePurgeCapabilities GetCapabilities()
        => new(false, "ManualOnly", "Monster database purge requires a privileged, separately reviewed operator workflow.");

    public Task<TenantDatabasePurgeResult> PurgeAsync(
        TenantDatabasePurgeRequest request,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new TenantDatabasePurgeResult(false, "ManualMonster", "TENANT_DATABASE_PURGE_MANUAL_ONLY"));
}
