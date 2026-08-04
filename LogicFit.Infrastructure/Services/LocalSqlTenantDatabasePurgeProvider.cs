using LogicFit.Application.Common.Interfaces;
using LogicFit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LogicFit.Infrastructure.Services;

/// <summary>
/// Development-only purge provider for operator-registered local SQL resources. The database is
/// removed through EF's provider boundary; the resource row remains available and provisioning
/// recreates the schema from the approved tenant migration assembly.
/// </summary>
public sealed class LocalSqlTenantDatabasePurgeProvider(
    ApplicationDbContext platformDb,
    IConnectionStringProtector connectionStringProtector,
    IConfiguration configuration,
    IHostEnvironment environment,
    ILogger<LocalSqlTenantDatabasePurgeProvider> logger) : ITenantDatabasePurgeProvider
{
    public TenantDatabasePurgeCapabilities GetCapabilities()
    {
        var enabled = environment.IsDevelopment() &&
            configuration.GetValue("TenantLifecycle:PurgeEnabled", false) &&
            string.Equals(configuration["DatabaseResourcePool:ProvisioningProvider"], "LocalSql", StringComparison.OrdinalIgnoreCase);

        return new TenantDatabasePurgeCapabilities(
            enabled,
            enabled ? "LocalSql" : "Disabled",
            enabled ? null : "Tenant database purge is enabled only in Development with TenantLifecycle:PurgeEnabled=true and LocalSql resources.");
    }

    public async Task<TenantDatabasePurgeResult> PurgeAsync(
        TenantDatabasePurgeRequest request,
        CancellationToken cancellationToken = default)
    {
        var capabilities = GetCapabilities();
        if (!capabilities.Enabled)
            return new TenantDatabasePurgeResult(false, "LocalSql", "TENANT_DATABASE_PURGE_DISABLED");

        var mapping = await platformDb.TenantDatabaseMappings
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(x => x.TenantId == request.TenantId &&
                x.DatabaseResourceId == request.DatabaseResourceId && x.IsActive, cancellationToken);
        var resource = await platformDb.DatabaseResources
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(x => x.Id == request.DatabaseResourceId &&
                x.ReservedForTenantId == request.TenantId, cancellationToken);

        if (mapping is null || resource is null || string.IsNullOrWhiteSpace(mapping.EncryptedConnectionString))
            return new TenantDatabasePurgeResult(false, "LocalSql", "TENANT_DATABASE_MAPPING_NOT_FOUND");

        try
        {
            var connectionString = connectionStringProtector.Unprotect(mapping.EncryptedConnectionString);
            var options = new DbContextOptionsBuilder<TenantDbContext>();
            DbContextSqlServerOptions.UseTenantDatabase(options, connectionString);
            await using var tenantDb = new TenantDbContext(options.Options, request.TenantId);

            // EnsureDeleted delegates the destructive operation to the configured EF SQL provider.
            // It does not accept a database name or SQL fragment from the HTTP request.
            var deleted = await tenantDb.Database.EnsureDeletedAsync(cancellationToken);
            if (!deleted)
                return new TenantDatabasePurgeResult(false, "LocalSql", "TENANT_DATABASE_PURGE_NOT_CONFIRMED");

            return new TenantDatabasePurgeResult(true, "LocalSql", null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Local tenant database purge failed for TenantId {TenantId} and ResourceId {ResourceId}.",
                request.TenantId, request.DatabaseResourceId);
            return new TenantDatabasePurgeResult(false, "LocalSql", "TENANT_DATABASE_PURGE_FAILED");
        }
    }
}
