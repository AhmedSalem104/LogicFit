namespace LogicFit.Infrastructure.Persistence;

/// <summary>
/// Controls the runtime cutover from the legacy shared context to the Platform/Tenant
/// contexts.  The default is fail-closed: once a tenant request reaches the routing middleware,
/// it must have an active Platform mapping before any tenant-owned DbSet can be used.
/// </summary>
public sealed class TenantDatabaseRoutingOptions
{
    public const string SectionName = "Database:TenantRouting";

    /// <summary>
    /// Enables request-time tenant database routing.  Set this to false only during a deliberate
    /// data migration/rollback window; it is not a normal application fallback.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Returns 503 when a non-platform request has no valid active mapping.  This must remain true
    /// in production so a missing mapping can never silently read the shared database.
    /// </summary>
    public bool FailClosedWithoutMapping { get; set; } = true;

    public static bool IsValid(TenantDatabaseRoutingOptions options)
        => options is not null;
}
