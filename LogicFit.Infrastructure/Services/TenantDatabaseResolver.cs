using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace LogicFit.Infrastructure.Services;

/// <summary>
/// Resolves a tenant connection only after the Platform DB confirms an active mapping to an
/// assigned resource reserved for the same tenant.  Protected connection material is decrypted
/// in memory and is never written to logs or returned by any API contract.
/// </summary>
public sealed class TenantDatabaseResolver(
    ITenantDatabaseMappingReader mappingReader,
    IConnectionStringProtector connectionStringProtector,
    ILogger<TenantDatabaseResolver> logger) : ITenantDatabaseResolver
{
    public async Task<TenantDatabaseResolution?> ResolveAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));

        var mapping = await mappingReader.FindActiveAsync(tenantId, cancellationToken);
        if (mapping is null)
            return null;

        // A stale mapping must fail closed.  Provisioning is responsible for changing both
        // resource state and the mapping atomically in the Platform workflow.
        if (mapping.ResourceStatus != DatabaseResourceStatus.Assigned ||
            mapping.ReservedForTenantId != tenantId ||
            string.IsNullOrWhiteSpace(mapping.EncryptedConnectionString))
        {
            logger.LogWarning(
                "Tenant database mapping {MappingId} is not assigned to its tenant; access was denied for TenantId {TenantId}.",
                mapping.MappingId,
                tenantId);
            return null;
        }

        string connectionString;
        try
        {
            connectionString = connectionStringProtector.Unprotect(mapping.EncryptedConnectionString);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            // Do not surface protected material or cryptographic details to a caller.  A
            // corrupted/rotated key is an unavailable tenant database, not a fallback signal.
            logger.LogError(
                exception,
                "Tenant database mapping {MappingId} could not be decrypted for TenantId {TenantId}.",
                mapping.MappingId,
                tenantId);
            return null;
        }

        if (string.IsNullOrWhiteSpace(connectionString))
            return null;

        return new TenantDatabaseResolution(
            tenantId,
            mapping.DatabaseResourceId,
            mapping.Provider,
            mapping.DatabaseName,
            connectionString,
            mapping.SchemaVersion,
            mapping.LastValidatedAtUtc);
    }
}
