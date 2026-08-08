using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Enums;
using LogicFit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace LogicFit.Infrastructure.HealthChecks;

/// <summary>
/// Detects encrypted mappings that the deployed key ring cannot read. Without this check the
/// application can report a healthy Platform DB while every affected workspace returns 503.
/// </summary>
public sealed class TenantDatabaseMappingHealthCheck(
    PlatformDbContext platformDb,
    IConnectionStringProtector connectionStringProtector,
    ILogger<TenantDatabaseMappingHealthCheck> logger) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var resourceValues = await platformDb.DatabaseResources
                .AsNoTracking()
                // A faulted/retired/unallocated pool row is not used by request routing or a
                // FullSystem backup. It is surfaced as a pool-operation problem instead of
                // making the entire API readiness probe fail. Reserved, provisioning, and
                // assigned resources remain fail-closed because they are runtime dependencies.
                .Where(resource => resource.Status == DatabaseResourceStatus.Reserved ||
                    resource.Status == DatabaseResourceStatus.Provisioning ||
                    resource.Status == DatabaseResourceStatus.Assigned)
                .Where(resource => !string.IsNullOrWhiteSpace(resource.EncryptedConnectionString))
                .Select(resource => new ProtectedValue(
                    "database resource",
                    resource.Id,
                    resource.Id,
                    null,
                    resource.DatabaseName,
                    resource.EncryptedConnectionString!))
                .ToListAsync(cancellationToken);

            var mappingValues = await platformDb.TenantDatabaseMappings
                .AsNoTracking()
                .Where(mapping => mapping.IsActive && !string.IsNullOrWhiteSpace(mapping.EncryptedConnectionString))
                .Select(mapping => new ProtectedValue(
                    "tenant database mapping",
                    mapping.Id,
                    mapping.DatabaseResourceId,
                    mapping.TenantId,
                    null,
                    mapping.EncryptedConnectionString))
                .ToListAsync(cancellationToken);

            var protectedValues = resourceValues.Concat(mappingValues).ToArray();
            foreach (var protectedValue in protectedValues)
            {
                try
                {
                    var connectionString = connectionStringProtector.Unprotect(protectedValue.Value);
                    if (string.IsNullOrWhiteSpace(connectionString))
                    {
                        logger.LogError(
                            "A protected {ProtectedValueType} row {ProtectedValueId} for resource {DatabaseResourceId} and tenant {TenantId} decrypted to an empty connection string.",
                            protectedValue.Type,
                            protectedValue.RowId,
                            protectedValue.DatabaseResourceId,
                            protectedValue.TenantId);
                        return HealthCheckResult.Unhealthy("A protected tenant database value decrypted to an empty connection string.");
                    }
                }
                catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or System.Security.Cryptography.CryptographicException)
                {
                    logger.LogError(
                        exception,
                        "The Data Protection key ring cannot decrypt a protected {ProtectedValueType} row {ProtectedValueId} for resource {DatabaseResourceId} and tenant {TenantId} (database {DatabaseName}).",
                        protectedValue.Type,
                        protectedValue.RowId,
                        protectedValue.DatabaseResourceId,
                        protectedValue.TenantId,
                        protectedValue.DatabaseName);
                    return HealthCheckResult.Unhealthy(
                        "The Data Protection key ring cannot decrypt all protected tenant database values.");
                }
            }

            return HealthCheckResult.Healthy($"Validated {protectedValues.Length} protected tenant database value(s).");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "The tenant database mapping health check failed.");
            return HealthCheckResult.Unhealthy("Tenant database mapping health could not be verified.");
        }
    }

    private sealed record ProtectedValue(
        string Type,
        Guid RowId,
        Guid? DatabaseResourceId,
        Guid? TenantId,
        string? DatabaseName,
        string Value);
}
