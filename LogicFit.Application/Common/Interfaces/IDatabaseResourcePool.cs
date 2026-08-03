using LogicFit.Domain.Enums;

namespace LogicFit.Application.Common.Interfaces;

public sealed record DatabaseResourceReservation(
    Guid ResourceId,
    Guid TenantId,
    string Provider,
    string DatabaseName,
    DateTime ReservedAtUtc);

public interface IDatabaseResourcePool
{
    Task<DatabaseResourceReservation?> ReserveAvailableAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<bool> ReleaseAsync(Guid resourceId, Guid tenantId, CancellationToken cancellationToken = default);
}

public interface IConnectionStringProtector
{
    string Protect(string connectionString);
    string Unprotect(string protectedConnectionString);
}

public sealed record DatabaseProvisioningResult(
    string Status,
    Guid TenantId,
    Guid? ResourceId,
    string? Provider,
    string? DatabaseName,
    string? ErrorCode = null,
    Guid? LocalUserId = null,
    string? SchemaVersion = null);

public interface IDatabaseProvisioningProvider
{
    string ProviderName { get; }
    Task<DatabaseProvisioningResult> ProvisionAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
