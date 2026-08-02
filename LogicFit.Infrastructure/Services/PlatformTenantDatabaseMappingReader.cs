using LogicFit.Application.Common.Interfaces;
using LogicFit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Infrastructure.Services;

/// <summary>
/// Reads the active mapping and its resource exclusively from Platform DB.  It intentionally
/// does not accept a database identifier or connection string from a request.
/// </summary>
public sealed class PlatformTenantDatabaseMappingReader(ApplicationDbContext dbContext) : ITenantDatabaseMappingReader
{
    public Task<TenantDatabaseMappingRecord?> FindActiveAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));

        return dbContext.TenantDatabaseMappings
            .AsNoTracking()
            .Where(mapping => mapping.TenantId == tenantId && mapping.IsActive)
            .Join(
                dbContext.DatabaseResources.AsNoTracking(),
                mapping => mapping.DatabaseResourceId,
                resource => resource.Id,
                (mapping, resource) => new TenantDatabaseMappingRecord(
                    mapping.Id,
                    mapping.TenantId,
                    mapping.DatabaseResourceId,
                    mapping.Provider,
                    resource.DatabaseName,
                    resource.Status,
                    resource.ReservedForTenantId,
                    mapping.EncryptedConnectionString,
                    mapping.SchemaVersion,
                    mapping.LastValidatedAtUtc))
            .SingleOrDefaultAsync(cancellationToken);
    }
}
