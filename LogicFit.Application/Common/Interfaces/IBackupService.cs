using LogicFit.Domain.Enums;

namespace LogicFit.Application.Common.Interfaces;

public sealed record BackupRecord(string FileName, long SizeBytes, DateTimeOffset CreatedAt, string Status);

public sealed record BackupDownload(string FileName, long SizeBytes, Stream Content);

public sealed record BackupStatus(
    bool IsEnabled,
    bool IsReady,
    string Format,
    int RetentionDays,
    string RunAtUtc,
    int BackupCount,
    string? UnavailableReason);

public sealed record BackupBatchRequest(
    BackupScope Scope,
    IReadOnlyCollection<Guid>? TenantIds = null,
    string? IdempotencyKey = null,
    bool IncludePlatform = true);

public sealed record BackupArtifactDto(
    Guid Id,
    Guid? TenantId,
    string Status,
    long SizeBytes,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    string? StorageKey,
    string? Sha256,
    string? ErrorCode,
    string? TenantName = null,
    string? WorkspaceIdentifier = null,
    string? WorkspaceType = null);

public sealed record BackupBatchDto(
    Guid Id,
    BackupScope Scope,
    string Status,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    string? ManifestStorageKey,
    IReadOnlyList<BackupArtifactDto> Artifacts);

public interface IBackupService
{
    Task<BackupRecord> CreateAsync(CancellationToken cancellationToken);
    IReadOnlyList<BackupRecord> List();
    BackupStatus GetStatus();
    BackupDownload OpenRead(string fileName);
    Task<BackupBatchDto> CreateBatchAsync(BackupBatchRequest request, CancellationToken cancellationToken);
    Task<BackupBatchDto> RetryBatchAsync(Guid batchId, CancellationToken cancellationToken);
    IReadOnlyList<BackupBatchDto> ListBatches(int take = 50);
}

/// <summary>Creates a recoverable archive of locally stored media.</summary>
public interface IMediaBackupService
{
    Task<BackupRecord> CreateAsync(CancellationToken cancellationToken);
}
