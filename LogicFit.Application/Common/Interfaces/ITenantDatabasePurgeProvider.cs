namespace LogicFit.Application.Common.Interfaces;

/// <summary>
/// Provider boundary for destroying the private database assigned to one workspace.
/// Providers must be explicitly enabled for the environment; the application never builds
/// destructive SQL from an HTTP request.
/// </summary>
public sealed record TenantDatabasePurgeCapabilities(
    bool Enabled,
    string Mode,
    string? UnavailableReason);

public sealed record TenantDatabasePurgeRequest(
    Guid TenantId,
    Guid DatabaseResourceId,
    string Provider);

public sealed record TenantDatabasePurgeResult(
    bool Succeeded,
    string Provider,
    string? ErrorCode);

public interface ITenantDatabasePurgeProvider
{
    TenantDatabasePurgeCapabilities GetCapabilities();
    Task<TenantDatabasePurgeResult> PurgeAsync(
        TenantDatabasePurgeRequest request,
        CancellationToken cancellationToken = default);
}
