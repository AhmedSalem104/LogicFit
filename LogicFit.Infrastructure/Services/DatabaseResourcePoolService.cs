using System.Data;
using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Enums;
using LogicFit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LogicFit.Infrastructure.Services;

/// <summary>Serializes resource reservations in Platform DB; no frontend-supplied database data is used.</summary>
public sealed class DatabaseResourcePoolService(
    PlatformDbContext dbContext,
    IDateTimeService dateTime,
    ILogger<DatabaseResourcePoolService> logger) : IDatabaseResourcePool
{
    public async Task<DatabaseResourceReservation?> ReserveAvailableAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));

        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var resource = await dbContext.DatabaseResources
            .Where(x => x.Status == DatabaseResourceStatus.Available)
            .OrderBy(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (resource is null)
        {
            logger.LogWarning("No available tenant database resource exists for TenantId {TenantId}.", tenantId);
            return null;
        }

        var reservedAt = dateTime.UtcNow;
        resource.Status = DatabaseResourceStatus.Reserved;
        resource.ReservedForTenantId = tenantId;
        resource.ReservedAtUtc = reservedAt;
        resource.LastError = null;
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new DatabaseResourceReservation(resource.Id, tenantId, resource.Provider, resource.DatabaseName, reservedAt);
    }

    public async Task<bool> ReleaseAsync(Guid resourceId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var resource = await dbContext.DatabaseResources
            .SingleOrDefaultAsync(x => x.Id == resourceId && x.ReservedForTenantId == tenantId, cancellationToken);
        // Assigned resources are released only after the tenant-database purge provider has
        // completed successfully. Maintenance is accepted for an operator-recovered resource.
        if (resource is null || resource.Status is not (DatabaseResourceStatus.Reserved or
            DatabaseResourceStatus.Provisioning or DatabaseResourceStatus.Assigned or DatabaseResourceStatus.Maintenance))
            return false;

        resource.Status = DatabaseResourceStatus.Available;
        resource.ReservedForTenantId = null;
        resource.ReservedAtUtc = null;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
