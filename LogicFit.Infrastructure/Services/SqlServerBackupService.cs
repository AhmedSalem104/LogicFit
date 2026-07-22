using System.Data.Common;
using LogicFit.Application.Common.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LogicFit.Infrastructure.Services;

public sealed class SqlServerBackupService : IBackupService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SqlServerBackupService> _logger;

    public SqlServerBackupService(IConfiguration configuration, ILogger<SqlServerBackupService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public IReadOnlyList<BackupRecord> List()
    {
        var directory = GetDirectory();
        if (!Directory.Exists(directory)) return [];
        return Directory.EnumerateFiles(directory, "*.bak", SearchOption.TopDirectoryOnly)
            .Select(path => new FileInfo(path))
            .OrderByDescending(file => file.CreationTimeUtc)
            .Select(file => new BackupRecord(file.Name, file.Length, file.CreationTimeUtc, "Completed"))
            .ToList();
    }

    public async Task<BackupRecord> CreateAsync(CancellationToken cancellationToken)
    {
        var directory = GetDirectory();
        Directory.CreateDirectory(directory);
        var database = new SqlConnectionStringBuilder(_configuration.GetConnectionString("DefaultConnection"))
            .InitialCatalog;
        if (string.IsNullOrWhiteSpace(database)) throw new InvalidOperationException("Database name is not configured.");

        var fileName = $"{Sanitize(database)}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.bak";
        var path = Path.Combine(directory, fileName);
        var escapedPath = path.Replace("'", "''", StringComparison.Ordinal);
        await using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandTimeout = _configuration.GetValue("Backup:CommandTimeoutSeconds", 3600);
        command.CommandText = $"BACKUP DATABASE [{database.Replace("]", "]]", StringComparison.Ordinal)}] TO DISK = N'{escapedPath}' WITH INIT, COMPRESSION, CHECKSUM, STATS = 10";
        await command.ExecuteNonQueryAsync(cancellationToken);

        var retentionDays = Math.Clamp(_configuration.GetValue("Backup:RetentionDays", 7), 1, 30);
        foreach (var old in Directory.EnumerateFiles(directory, "*.bak").Select(x => new FileInfo(x))
                     .Where(x => x.CreationTimeUtc < DateTime.UtcNow.AddDays(-retentionDays)))
        {
            try { old.Delete(); } catch (Exception ex) { _logger.LogWarning(ex, "Unable to remove expired backup file {FileName}", old.Name); }
        }
        var info = new FileInfo(path);
        return new BackupRecord(info.Name, info.Length, info.CreationTimeUtc, "Completed");
    }

    private string GetDirectory() => _configuration["Backup:Directory"]
        ?? throw new InvalidOperationException("Backup:Directory is not configured.");

    private static string Sanitize(string value) => string.Concat(value.Select(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_' ? ch : '_'));
}
