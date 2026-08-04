using LogicFit.Application.Common.Interfaces;
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
                .Where(resource => !string.IsNullOrWhiteSpace(resource.EncryptedConnectionString))
                .Select(resource => new ProtectedValue(
                    "database resource",
                    resource.EncryptedConnectionString!))
                .ToListAsync(cancellationToken);

            var mappingValues = await platformDb.TenantDatabaseMappings
                .AsNoTracking()
                .Where(mapping => mapping.IsActive && !string.IsNullOrWhiteSpace(mapping.EncryptedConnectionString))
                .Select(mapping => new ProtectedValue(
                    "tenant database mapping",
                    mapping.EncryptedConnectionString))
                .ToListAsync(cancellationToken);

            var protectedValues = resourceValues.Concat(mappingValues).ToArray();
            foreach (var protectedValue in protectedValues)
            {
                try
                {
                    var connectionString = connectionStringProtector.Unprotect(protectedValue.Value);
                    if (string.IsNullOrWhiteSpace(connectionString))
                        return HealthCheckResult.Unhealthy("A protected tenant database value decrypted to an empty connection string.");
                }
                catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or System.Security.Cryptography.CryptographicException)
                {
                    logger.LogError(
                        exception,
                        "The Data Protection key ring cannot decrypt a protected {ProtectedValueType}.",
                        protectedValue.Type);
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

    private sealed record ProtectedValue(string Type, string Value);
}
