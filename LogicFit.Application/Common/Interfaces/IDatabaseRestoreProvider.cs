using LogicFit.Domain.Enums;

namespace LogicFit.Application.Common.Interfaces;

public sealed record DatabaseRestoreCapabilities(
    bool Enabled,
    string Mode,
    bool SupportsBacpacImport,
    bool SupportsMappingSwitch,
    string? UnavailableReason);

public sealed record DatabaseRestoreRequest(
    Guid TenantId,
    Guid SourceDatabaseBackupId,
    Guid? TargetDatabaseResourceId,
    string WorkspaceNameConfirmation,
    string Reason);

public sealed record DatabaseRestoreResult(
    bool Succeeded,
    Guid? TargetDatabaseResourceId,
    Guid? PreviousMappingId,
    string? ErrorCode,
    string Provider,
    string? SchemaVersion = null);

public interface IDatabaseRestoreProvider
{
    DatabaseRestoreCapabilities GetCapabilities();
    Task<DatabaseRestoreResult> RestoreAsync(DatabaseRestoreRequest request, CancellationToken cancellationToken = default);
}

public sealed record RestoreJobDto(
    Guid Id,
    Guid TenantId,
    RestoreJobStatus Status,
    string Provider,
    DateTime CreatedAtUtc,
    DateTime? StartedAtUtc,
    DateTime? CompletedAtUtc,
    string? ErrorCode);

public interface IDatabaseRestoreService
{
    DatabaseRestoreCapabilities GetCapabilities();
    Task<SensitiveActionGrantDto> ReauthenticateAsync(Guid userId, string currentPassword, CancellationToken cancellationToken = default);
    Task<RestoreJobDto> RestoreAsync(Guid userId, string grantToken, DatabaseRestoreRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RestoreJobDto>> ListAsync(CancellationToken cancellationToken = default);
}
