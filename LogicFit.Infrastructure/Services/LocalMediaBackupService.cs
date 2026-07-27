using System.IO.Compression;
using LogicFit.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LogicFit.Infrastructure.Services;

/// <summary>Archives wwwroot/uploads into the same private App_Data backup directory as BACPACs.</summary>
public sealed class LocalMediaBackupService : IMediaBackupService
{
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<LocalMediaBackupService> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public LocalMediaBackupService(IConfiguration configuration, IHostEnvironment environment, ILogger<LocalMediaBackupService> logger, TimeProvider timeProvider)
    {
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task<BackupRecord> CreateAsync(CancellationToken cancellationToken)
    {
        if (!_configuration.GetValue("Backup:Enabled", false))
            throw new InvalidOperationException("Backup is disabled.");
        if (!await _lock.WaitAsync(0, cancellationToken))
            throw new InvalidOperationException("A media backup is already running.");

        string? temporary = null;
        try
        {
            var contentRoot = string.IsNullOrWhiteSpace(_environment.ContentRootPath) ? AppContext.BaseDirectory : _environment.ContentRootPath;
            var uploads = Path.GetFullPath(Path.Combine(contentRoot, "wwwroot", "uploads"));
            if (!Directory.Exists(uploads))
                throw new DirectoryNotFoundException("The media directory wwwroot/uploads does not exist.");

            var configured = _configuration["Backup:StorageDirectory"]?.Trim();
            if (string.IsNullOrWhiteSpace(configured) || Path.IsPathRooted(configured))
                throw new InvalidOperationException("Backup:StorageDirectory must be a relative path inside App_Data.");
            var appData = Path.GetFullPath(Path.Combine(contentRoot, "App_Data"));
            var destination = Path.GetFullPath(Path.Combine(contentRoot, configured));
            var prefix = appData.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!destination.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Media backup destination must remain inside App_Data.");

            Directory.CreateDirectory(destination);
            var now = _timeProvider.GetUtcNow();
            var fileName = $"media-{now:yyyyMMdd-HHmmss}.zip";
            var path = Path.Combine(destination, fileName);
            temporary = path + ".partial";
            await Task.Run(() => ZipFile.CreateFromDirectory(uploads, temporary, CompressionLevel.Optimal, includeBaseDirectory: false), cancellationToken);
            File.Move(temporary, path);
            temporary = null;

            var retention = Math.Clamp(_configuration.GetValue("Backup:RetentionDays", 7), 1, 30);
            var cutoff = now.UtcDateTime.AddDays(-retention);
            foreach (var old in Directory.EnumerateFiles(destination, "media-*.zip", SearchOption.TopDirectoryOnly))
                if (File.GetCreationTimeUtc(old) < cutoff) File.Delete(old);

            var info = new FileInfo(path);
            return new BackupRecord(info.Name, info.Length, info.CreationTimeUtc, "Completed");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Local media backup failed");
            throw new InvalidOperationException("Unable to create the media backup.", ex);
        }
        finally
        {
            if (temporary is not null) TryDelete(temporary);
            _lock.Release();
        }
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { /* preserve the original failure */ }
    }
}
