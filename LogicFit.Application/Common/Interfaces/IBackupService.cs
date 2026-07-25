namespace LogicFit.Application.Common.Interfaces;

public sealed record BackupRecord(string FileName, long SizeBytes, DateTimeOffset CreatedAt, string Status);

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
}
