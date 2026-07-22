namespace LogicFit.Application.Common.Interfaces;

public sealed record BackupRecord(string FileName, long SizeBytes, DateTimeOffset CreatedAt, string Status);

public interface IBackupService
{
    Task<BackupRecord> CreateAsync(CancellationToken cancellationToken);
    IReadOnlyList<BackupRecord> List();
}
