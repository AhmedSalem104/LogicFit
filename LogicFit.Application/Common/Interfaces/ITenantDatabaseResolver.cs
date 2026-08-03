using LogicFit.Domain.Enums;

namespace LogicFit.Application.Common.Interfaces;

/// <summary>
/// A platform-owned, server-side view of an assigned tenant database.  This type is an
/// infrastructure boundary and must never be serialized as an API response.
/// </summary>
public sealed record TenantDatabaseResolution(
    Guid TenantId,
    Guid DatabaseResourceId,
    string Provider,
    string DatabaseName,
    string ConnectionString,
    string? SchemaVersion,
    DateTime? LastValidatedAtUtc);

/// <summary>
/// Resolves an active workspace database from the central mapping.  The only caller input is
/// the tenant id obtained from an authenticated server-side context; database names and
/// connection strings are never accepted from HTTP clients.
/// </summary>
public interface ITenantDatabaseResolver
{
    Task<TenantDatabaseResolution?> ResolveAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Projection used by the infrastructure reader so the resolver never exposes an EF entity
/// or navigation graph to application/API code.
/// </summary>
public sealed record TenantDatabaseMappingRecord(
    Guid MappingId,
    Guid TenantId,
    Guid DatabaseResourceId,
    string Provider,
    string DatabaseName,
    DatabaseResourceStatus ResourceStatus,
    Guid? ReservedForTenantId,
    string EncryptedConnectionString,
    string? SchemaVersion,
    DateTime? LastValidatedAtUtc);

public interface ITenantDatabaseMappingReader
{
    Task<TenantDatabaseMappingRecord?> FindActiveAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
