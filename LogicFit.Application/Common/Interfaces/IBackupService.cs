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

public interface IBackupService
{
    Task<BackupRecord> CreateAsync(CancellationToken cancellationToken);
    IReadOnlyList<BackupRecord> List();
    BackupStatus GetStatus();
    BackupDownload OpenRead(string fileName);
}

/// <summary>Creates a recoverable archive of locally stored media.</summary>
public interface IMediaBackupService
{
    Task<BackupRecord> CreateAsync(CancellationToken cancellationToken);
}
