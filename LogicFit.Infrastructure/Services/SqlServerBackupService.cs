using LogicFit.Application.Common.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.SqlServer.Dac;
using System.Text.RegularExpressions;

namespace LogicFit.Infrastructure.Services;

/// <summary>
/// Creates portable BACPAC exports containing the current database schema and data.
/// The export is written by the application to a private App_Data folder, so it works
/// with shared SQL hosting where the database server cannot write to the web site disk.
/// </summary>
public sealed class SqlServerBackupService : IBackupService
{
    private const string BackupSearchPattern = "*.bacpac";
    private static readonly Regex BackupFileNamePattern = new(
        "^[A-Za-z0-9][A-Za-z0-9_-]{0,127}-\\d{8}-\\d{6}\\.bacpac$",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase,
        TimeSpan.FromSeconds(1));
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<SqlServerBackupService> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim _exportLock = new(1, 1);

    public SqlServerBackupService(
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger<SqlServerBackupService> logger,
        TimeProvider timeProvider)
    {
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public IReadOnlyList<BackupRecord> List()
    {
        if (!TryGetSettings(out var settings, out _)) return [];

        try
        {
            if (!Directory.Exists(settings.StorageDirectory)) return [];

            return Directory.EnumerateFiles(settings.StorageDirectory, BackupSearchPattern, SearchOption.TopDirectoryOnly)
                .Select(path => new FileInfo(path))
                .OrderByDescending(file => file.CreationTimeUtc)
                .Select(file => new BackupRecord(file.Name, file.Length, file.CreationTimeUtc, "Completed"))
                .ToList();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning("The private backup storage cannot be listed.");
            return [];
        }
    }

    public BackupStatus GetStatus()
    {
        var enabled = _configuration.GetValue("Backup:Enabled", false);
        var retentionDays = Math.Clamp(_configuration.GetValue("Backup:RetentionDays", 7), 1, 30);
        var runAtUtc = GetRunAtUtc().ToString("hh\\:mm");

        if (!TryGetSettings(out var settings, out var reason))
        {
            return new BackupStatus(enabled, false, "BACPAC", retentionDays, runAtUtc, 0, reason);
        }

        try
        {
            var count = Directory.Exists(settings.StorageDirectory)
                ? Directory.EnumerateFiles(settings.StorageDirectory, BackupSearchPattern, SearchOption.TopDirectoryOnly).Count()
                : 0;

            return new BackupStatus(true, true, "BACPAC", settings.RetentionDays, settings.RunAtUtc.ToString("hh\\:mm"), count, null);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning("The private backup storage cannot be inspected.");
            return new BackupStatus(true, false, "BACPAC", retentionDays, runAtUtc, 0, "تعذر الوصول إلى مساحة التخزين الخاصة بالنسخ الاحتياطي.");
        }
    }

    public BackupDownload OpenRead(string fileName)
    {
        if (!TryGetSettings(out var settings, out var reason))
        {
            throw new InvalidOperationException(reason);
        }

        if (!IsSafeBackupFileName(fileName))
        {
            throw new FileNotFoundException("Backup file was not found.");
        }

        var path = Path.Combine(settings.StorageDirectory, fileName);
        try
        {
            var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 64 * 1024, useAsync: true);
            return new BackupDownload(fileName, stream.Length, stream);
        }
        catch (FileNotFoundException)
        {
            throw;
        }
        catch (DirectoryNotFoundException)
        {
            throw new FileNotFoundException("Backup file was not found.");
        }
        catch (UnauthorizedAccessException)
        {
            _logger.LogWarning("A private BACPAC download was requested while storage was unavailable.");
            throw new InvalidOperationException("تعذر الوصول إلى مساحة التخزين الخاصة بالنسخ الاحتياطي.");
        }
        catch (IOException)
        {
            _logger.LogWarning("A private BACPAC download could not be opened.");
            throw new InvalidOperationException("تعذر فتح النسخة الاحتياطية المطلوبة.");
        }
    }

    public async Task<BackupRecord> CreateAsync(CancellationToken cancellationToken)
    {
        if (!TryGetSettings(out var settings, out var reason))
        {
            throw new InvalidOperationException(reason);
        }

        if (!await _exportLock.WaitAsync(0, cancellationToken))
        {
            throw new InvalidOperationException("توجد عملية نسخ احتياطي قيد التنفيذ بالفعل.");
        }

        string? temporaryPath = null;
        try
        {
            Directory.CreateDirectory(settings.StorageDirectory);
            var now = _timeProvider.GetUtcNow();
            var fileName = $"{Sanitize(settings.DatabaseName)}-{now:yyyyMMdd-HHmmss}.bacpac";
            var destinationPath = Path.Combine(settings.StorageDirectory, fileName);
            temporaryPath = destinationPath + ".partial";

            var dacServices = new DacServices(settings.ConnectionString);
            await Task.Run(
                () => dacServices.ExportBacpac(
                    temporaryPath,
                    settings.DatabaseName,
                    (IEnumerable<Tuple<string, string>>?)null,
                    cancellationToken),
                CancellationToken.None);

            File.Move(temporaryPath, destinationPath);
            temporaryPath = null;
            PruneExpiredBackups(settings.StorageDirectory, settings.RetentionDays, now);

            var file = new FileInfo(destinationPath);
            return new BackupRecord(file.Name, file.Length, file.CreationTimeUtc, "Completed");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError("BACPAC export failed with {ExceptionType}.", ex.GetType().Name);
            throw new InvalidOperationException("تعذر إنشاء النسخة الاحتياطية. تحقق من صلاحيات مستخدم قاعدة البيانات ومساحة الاستضافة ثم أعد المحاولة.");
        }
        finally
        {
            if (temporaryPath is not null)
            {
                TryDeletePartial(temporaryPath);
            }

            _exportLock.Release();
        }
    }

    private bool TryGetSettings(out BackupSettings settings, out string reason)
    {
        settings = default!;
        reason = "";

        if (!_configuration.GetValue("Backup:Enabled", false))
        {
            reason = "النسخ الاحتياطي غير مفعّل على الخادم. فعّل Backup:Enabled بعد نشر الإصدار الجديد.";
            return false;
        }

        var configuredDirectory = _configuration["Backup:StorageDirectory"]?.Trim();
        if (string.IsNullOrWhiteSpace(configuredDirectory))
        {
            reason = "يلزم ضبط Backup:StorageDirectory داخل App_Data على الخادم.";
            return false;
        }

        if (Path.IsPathRooted(configuredDirectory))
        {
            reason = "مسار النسخ يجب أن يكون مسارًا نسبيًا وآمنًا داخل App_Data.";
            return false;
        }

        var contentRoot = string.IsNullOrWhiteSpace(_environment.ContentRootPath)
            ? AppContext.BaseDirectory
            : _environment.ContentRootPath;
        var appDataRoot = Path.GetFullPath(Path.Combine(contentRoot, "App_Data"));
        var storageDirectory = Path.GetFullPath(Path.Combine(contentRoot, configuredDirectory));
        var appDataPrefix = appDataRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;

        if (!storageDirectory.StartsWith(appDataPrefix, StringComparison.OrdinalIgnoreCase))
        {
            reason = "مسار النسخ يجب أن يبقى داخل App_Data حتى لا يصبح متاحًا علنًا.";
            return false;
        }

        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            reason = "اتصال قاعدة البيانات غير مهيأ.";
            return false;
        }

        try
        {
            var databaseName = new SqlConnectionStringBuilder(connectionString).InitialCatalog;
            if (string.IsNullOrWhiteSpace(databaseName))
            {
                reason = "اسم قاعدة البيانات غير مهيأ.";
                return false;
            }

            settings = new BackupSettings(
                connectionString,
                databaseName,
                storageDirectory,
                Math.Clamp(_configuration.GetValue("Backup:RetentionDays", 7), 1, 30),
                GetRunAtUtc());
            return true;
        }
        catch (ArgumentException)
        {
            reason = "اتصال قاعدة البيانات غير صالح.";
            return false;
        }
    }

    private TimeSpan GetRunAtUtc() => TimeSpan.TryParse(_configuration["Backup:RunAtUtc"], out var configured)
        ? configured
        : new TimeSpan(2, 0, 0);

    private void PruneExpiredBackups(string directory, int retentionDays, DateTimeOffset now)
    {
        foreach (var oldFile in Directory.EnumerateFiles(directory, BackupSearchPattern, SearchOption.TopDirectoryOnly)
                     .Select(path => new FileInfo(path))
                     .Where(file => file.CreationTimeUtc < now.UtcDateTime.AddDays(-retentionDays)))
        {
            try
            {
                oldFile.Delete();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _logger.LogWarning("An expired BACPAC could not be removed.");
            }
        }
    }

    private static void TryDeletePartial(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch (Exception)
        {
            // The next successful export can be performed independently of an orphaned temporary file.
        }
    }

    private static string Sanitize(string value) => string.Concat(value.Select(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_' ? ch : '_'));

    private static bool IsSafeBackupFileName(string fileName) =>
        !string.IsNullOrWhiteSpace(fileName)
        && string.Equals(fileName, Path.GetFileName(fileName), StringComparison.Ordinal)
        && BackupFileNamePattern.IsMatch(fileName);

    private sealed record BackupSettings(
        string ConnectionString,
        string DatabaseName,
        string StorageDirectory,
        int RetentionDays,
        TimeSpan RunAtUtc);
}
