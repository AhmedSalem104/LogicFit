using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Common.Services;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.SqlServer.Dac;

namespace LogicFit.Infrastructure.Services;

/// <summary>
/// Central, platform-owned BACPAC orchestration.  Every target is exported to its own
/// private file and is resolved from the platform mapping; request payloads never contain
/// database names or connection strings.
/// </summary>
public sealed class DatabaseBackupService(
    ApplicationDbContext db,
    IConfiguration configuration,
    IHostEnvironment environment,
    IConnectionStringProtector connectionStringProtector,
    ICurrentUserService currentUser,
    IDateTimeService clock,
    ILogger<DatabaseBackupService> logger,
    TimeProvider timeProvider) : IBackupService
{
    private const string BackupSearchPattern = "*.bacpac";
    private static readonly SemaphoreSlim ProcessLock = new(1, 1);
    private static readonly Regex BackupFileNamePattern = new(
        "^[A-Za-z0-9][A-Za-z0-9_-]{0,127}-(?:\\d{8}-\\d{6}|[0-9a-fA-F]{8})\\.(?:bacpac|json)$",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase,
        TimeSpan.FromSeconds(1));

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
            logger.LogWarning("The private backup storage cannot be listed.");
            return [];
        }
    }

    public BackupStatus GetStatus()
    {
        var enabled = configuration.GetValue("Backup:Enabled", false);
        var retention = Math.Clamp(configuration.GetValue("Backup:RetentionDays", 7), 1, 3650);
        var runAt = GetRunAtUtc().ToString("hh\\:mm");
        if (!TryGetSettings(out var settings, out var reason))
            return new BackupStatus(enabled, false, "BACPAC", retention, runAt, 0, reason);

        try
        {
            var count = Directory.Exists(settings.StorageDirectory)
                ? Directory.EnumerateFiles(settings.StorageDirectory, BackupSearchPattern, SearchOption.TopDirectoryOnly).Count()
                : 0;
            return new BackupStatus(true, true, "BACPAC", settings.RetentionDays,
                settings.RunAtUtc.ToString("hh\\:mm"), count, null);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning("The private backup storage cannot be inspected.");
            return new BackupStatus(true, false, "BACPAC", retention, runAt, 0,
                "Private backup storage is unavailable.");
        }
    }

    public BackupDownload OpenRead(string fileName)
    {
        if (!TryGetSettings(out var settings, out var reason))
            throw new InvalidOperationException(reason);
        if (!IsSafeBackupFileName(fileName))
            throw new FileNotFoundException("Backup file was not found.");

        var path = Path.Combine(settings.StorageDirectory, fileName);
        try
        {
            var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
                bufferSize: 64 * 1024, useAsync: true);
            return new BackupDownload(fileName, stream.Length, stream);
        }
        catch (FileNotFoundException) { throw; }
        catch (DirectoryNotFoundException) { throw new FileNotFoundException("Backup file was not found."); }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            logger.LogWarning("A private backup download could not be opened.");
            throw new InvalidOperationException("Private backup storage is unavailable.");
        }
    }

    public async Task<BackupRecord> CreateAsync(CancellationToken cancellationToken)
    {
        var batch = await CreateBatchAsync(
            new BackupBatchRequest(BackupScope.Platform, IdempotencyKey: $"legacy-platform:{timeProvider.GetUtcNow():yyyyMMddHHmmss}-{Guid.NewGuid():N}"),
            cancellationToken);
        var artifact = batch.Artifacts.FirstOrDefault(x => x.Status == nameof(DatabaseBackupStatus.Completed));
        if (artifact is null || string.IsNullOrWhiteSpace(artifact.StorageKey))
            throw new InvalidOperationException("The platform database backup did not complete.");
        return new BackupRecord(artifact.StorageKey, artifact.SizeBytes,
            artifact.CompletedAtUtc ?? timeProvider.GetUtcNow(), artifact.Status);
    }

    public async Task<BackupBatchDto> CreateBatchAsync(BackupBatchRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!Enum.IsDefined(request.Scope))
            throw new ArgumentException("Backup scope is invalid.", nameof(request));
        if (request.Scope == BackupScope.SelectedTenants && (request.TenantIds is null || request.TenantIds.Count == 0))
            throw new ArgumentException("SelectedTenants requires at least one tenant.", nameof(request));
        if (request.TenantIds?.Any(id => id == Guid.Empty) == true)
            throw new ArgumentException("Tenant identifiers must be non-empty.", nameof(request));
        if (!TryGetSettings(out var settings, out var reason))
            throw new InvalidOperationException(reason);

        var idempotencyKey = string.IsNullOrWhiteSpace(request.IdempotencyKey)
            ? $"manual:{request.Scope}:{timeProvider.GetUtcNow():yyyyMMddHHmmss}-{Guid.NewGuid():N}"
            : request.IdempotencyKey.Trim();
        if (idempotencyKey.Length > 200)
            throw new ArgumentException("Idempotency key is too long.", nameof(request));

        var existing = await db.BackupBatches.AsNoTracking().Include(x => x.Artifacts)
            .SingleOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken);
        if (existing is not null) return await ToDtoAsync(existing, cancellationToken);

        if (!await ProcessLock.WaitAsync(0, cancellationToken))
            throw new InvalidOperationException("A backup batch is already running.");

        SqlConnection? distributedLockConnection = null;
        try
        {
            distributedLockConnection = await AcquireDistributedLockAsync(cancellationToken);
            existing = await db.BackupBatches.Include(x => x.Artifacts)
                .SingleOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken);
            if (existing is not null) return await ToDtoAsync(existing, cancellationToken);

            var targets = await ResolveTargetsAsync(request, cancellationToken);
            if (targets.Count == 0)
                throw new InvalidOperationException("No assigned tenant databases are available for this backup scope.");

            var now = timeProvider.GetUtcNow().UtcDateTime;
            var batch = new BackupBatch
            {
                Scope = request.Scope,
                Status = BackupBatchStatus.Running,
                StartedAtUtc = now,
                IdempotencyKey = idempotencyKey,
                Artifacts = targets.Select(target => new DatabaseBackup
                {
                    TenantId = target.TenantId,
                    DatabaseResourceId = target.DatabaseResourceId,
                    DatabaseName = target.DatabaseName,
                    Status = DatabaseBackupStatus.Running,
                    StartedAtUtc = now
                }).ToList()
            };
            db.BackupBatches.Add(batch);
            SecurityAuditLog.Add(db, currentUser, clock, "PlatformBackupBatchStarted", true, batch.Id);
            await db.SaveChangesAsync(cancellationToken);

            var maxConcurrent = Math.Clamp(configuration.GetValue("Backup:MaxConcurrent", 2), 1, 4);
            using var limiter = new SemaphoreSlim(maxConcurrent, maxConcurrent);
            var jobs = targets.Zip(batch.Artifacts, (target, artifact) => ExportTargetAsync(
                target, artifact.Id, settings, limiter, cancellationToken)).ToArray();
            var results = await Task.WhenAll(jobs);

            foreach (var result in results)
            {
                var artifact = batch.Artifacts.Single(x => x.Id == result.ArtifactId);
                artifact.Status = result.Status;
                artifact.StorageKey = result.StorageKey;
                artifact.SizeBytes = result.SizeBytes;
                artifact.Sha256 = result.Sha256;
                artifact.CompletedAtUtc = result.CompletedAtUtc;
                artifact.ErrorMessage = result.ErrorMessage;
            }

            var successful = results.Count(x => x.Status == DatabaseBackupStatus.Completed);
            batch.Status = successful == results.Length
                ? BackupBatchStatus.Completed
                : successful == 0 ? BackupBatchStatus.Failed : BackupBatchStatus.Partial;
            batch.CompletedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
            batch.ErrorMessage = successful == results.Length
                ? null
                : "One or more database exports failed; inspect individual artifact status.";

            var manifestName = $"manifest-{batch.Id:N}.json";
            var manifestPath = Path.Combine(settings.StorageDirectory, manifestName);
            await WriteManifestAsync(manifestPath, batch, cancellationToken);
            batch.ManifestStorageKey = manifestName;
            PruneExpiredBackups(settings.StorageDirectory, settings.RetentionDays);
            SecurityAuditLog.Add(db, currentUser, clock, "PlatformBackupBatchFinished",
                batch.Status == BackupBatchStatus.Completed, batch.Id);
            await db.SaveChangesAsync(cancellationToken);
            return await ToDtoAsync(batch, cancellationToken);
        }
        catch (DbUpdateException)
        {
            if (!await IsDuplicateIdempotencyAsync(idempotencyKey, cancellationToken)) throw;
            var duplicate = await db.BackupBatches.AsNoTracking().Include(x => x.Artifacts)
                .SingleAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken);
            return await ToDtoAsync(duplicate, cancellationToken);
        }
        finally
        {
            await ReleaseDistributedLockAsync(distributedLockConnection);
            ProcessLock.Release();
        }
    }

    public IReadOnlyList<BackupBatchDto> ListBatches(int take = 50)
    {
        take = Math.Clamp(take, 1, 100);
        var batches = db.BackupBatches.AsNoTracking().Include(x => x.Artifacts)
            .OrderByDescending(x => x.StartedAtUtc).Take(take).ToList();
        var tenantIds = batches.SelectMany(x => x.Artifacts)
            .Where(x => x.TenantId.HasValue)
            .Select(x => x.TenantId!.Value)
            .Distinct()
            .ToArray();
        var metadata = tenantIds.Length == 0
            ? new Dictionary<Guid, BackupTenantMetadata>()
            : db.Tenants.AsNoTracking().IgnoreQueryFilters()
                .Where(x => tenantIds.Contains(x.Id))
                .Select(x => new BackupTenantMetadata(x.Id, x.Name, x.Subdomain, x.WorkspaceType.ToString()))
                .ToDictionary(x => x.TenantId);
        return batches.Select(x => ToDto(x, metadata)).ToList();
    }

    public async Task<BackupBatchDto> RetryBatchAsync(Guid batchId, CancellationToken cancellationToken)
    {
        if (batchId == Guid.Empty) throw new ArgumentException("Batch id is required.", nameof(batchId));
        var batch = await db.BackupBatches.AsNoTracking().Include(x => x.Artifacts)
            .SingleOrDefaultAsync(x => x.Id == batchId, cancellationToken);
        if (batch is null) throw new KeyNotFoundException("Backup batch was not found.");
        if (batch.Status is not (BackupBatchStatus.Failed or BackupBatchStatus.Partial))
            throw new InvalidOperationException("Only failed or partial backup batches can be retried.");

        var failedArtifacts = batch.Artifacts
            .Where(x => x.Status != DatabaseBackupStatus.Completed)
            .ToList();
        var tenantIds = failedArtifacts.Where(x => x.TenantId.HasValue)
            .Select(x => x.TenantId!.Value).ToArray();
        return await CreateBatchAsync(
            new BackupBatchRequest(batch.Scope, tenantIds,
                $"retry:{batch.Id:N}:{timeProvider.GetUtcNow():yyyyMMddHHmmss}",
                IncludePlatform: failedArtifacts.Any(x => !x.TenantId.HasValue)),
            cancellationToken);
    }

    private async Task<IReadOnlyList<BackupTarget>> ResolveTargetsAsync(BackupBatchRequest request, CancellationToken cancellationToken)
    {
        var targets = new List<BackupTarget>();
        if (request.Scope == BackupScope.Platform ||
            (request.Scope == BackupScope.FullSystem && request.IncludePlatform))
        {
            var platformConnection = configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(platformConnection))
                throw new InvalidOperationException("Platform database connection is not configured.");
            var builder = new SqlConnectionStringBuilder(platformConnection);
            if (string.IsNullOrWhiteSpace(builder.InitialCatalog))
                throw new InvalidOperationException("Platform database name is not configured.");
            targets.Add(new BackupTarget(null, null, builder.InitialCatalog, platformConnection));
        }

        if (request.Scope == BackupScope.Platform) return targets;

        var mappings = await db.TenantDatabaseMappings.AsNoTracking()
            .Where(x => x.IsActive && !string.IsNullOrWhiteSpace(x.EncryptedConnectionString))
            .Join(db.DatabaseResources.AsNoTracking().Where(x => x.Status == DatabaseResourceStatus.Assigned),
                mapping => mapping.DatabaseResourceId, resource => resource.Id,
                (mapping, resource) => new { mapping, resource })
            // A SelectedTenants request is an explicit platform operation and may be used for a
            // final-delete safety backup of a previously soft-deleted tenant. Broad scheduled
            // scopes continue to exclude deleted tenants.
            .Join(db.Tenants.AsNoTracking().IgnoreQueryFilters()
                    .Where(x => request.Scope == BackupScope.SelectedTenants || !x.IsDeleted),
                pair => pair.mapping.TenantId, tenant => tenant.Id,
                (pair, tenant) => new { pair.mapping, pair.resource, tenant })
            .ToListAsync(cancellationToken);

        IEnumerable<dynamic> filtered = mappings;
        if (request.Scope == BackupScope.SelectedTenants)
            filtered = mappings.Where(x => request.TenantIds!.Contains(x.mapping.TenantId));
        else if (request.Scope == BackupScope.AllGyms)
            filtered = mappings.Where(x => x.tenant.WorkspaceType == WorkspaceType.Gym);
        else if (request.Scope == BackupScope.AllFreelance)
            filtered = mappings.Where(x => x.tenant.WorkspaceType == WorkspaceType.FreelanceCoach);
        else if (request.Scope is BackupScope.AllTenants or BackupScope.FullSystem)
            filtered = mappings;

        if (request.Scope != BackupScope.SelectedTenants && request.TenantIds is { Count: > 0 })
            filtered = filtered.Where(x => request.TenantIds!.Contains((Guid)x.mapping.TenantId));

        foreach (var item in filtered)
        {
            try
            {
                targets.Add(new BackupTarget(
                    item.mapping.TenantId,
                    item.resource.Id,
                    item.resource.DatabaseName,
                    connectionStringProtector.Unprotect(item.mapping.EncryptedConnectionString)));
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                logger.LogWarning("A tenant database mapping could not be resolved for backup.");
            }
        }
        return targets;
    }

    private async Task<ExportResult> ExportTargetAsync(
        BackupTarget target,
        Guid artifactId,
        BackupSettings settings,
        SemaphoreSlim limiter,
        CancellationToken cancellationToken)
    {
        await limiter.WaitAsync(cancellationToken);
        string? temporaryPath = null;
        try
        {
            var now = timeProvider.GetUtcNow();
            var fileName = $"{Sanitize(target.DatabaseName)}-{now:yyyyMMdd-HHmmss}-{artifactId:N}.bacpac";
            var destinationPath = Path.Combine(settings.StorageDirectory, fileName);
            temporaryPath = destinationPath + ".partial";
            Directory.CreateDirectory(settings.StorageDirectory);
            var dacServices = new DacServices(target.ConnectionString);
            await Task.Run(() => dacServices.ExportBacpac(
                temporaryPath, target.DatabaseName, (IEnumerable<Tuple<string, string>>?)null, cancellationToken),
                CancellationToken.None);
            File.Move(temporaryPath, destinationPath);
            temporaryPath = null;
            var file = new FileInfo(destinationPath);
            var hash = await ComputeSha256Async(destinationPath, cancellationToken);
            return new ExportResult(artifactId, DatabaseBackupStatus.Completed, file.Name, file.Length,
                hash, now.UtcDateTime, timeProvider.GetUtcNow().UtcDateTime, null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            logger.LogWarning("BACPAC export failed for one target: {ExceptionType}.", ex.GetType().Name);
            return new ExportResult(artifactId, DatabaseBackupStatus.Failed, null, 0, null,
                null, timeProvider.GetUtcNow().UtcDateTime, "DATABASE_EXPORT_FAILED");
        }
        finally
        {
            if (temporaryPath is not null) TryDelete(temporaryPath);
            limiter.Release();
        }
    }

    private async Task<SqlConnection?> AcquireDistributedLockAsync(CancellationToken cancellationToken)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString)) return null;
        var connection = new SqlConnection(connectionString);
        try
        {
            await connection.OpenAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = "EXEC @result = sp_getapplock @Resource = @resource, @LockMode = 'Exclusive', @LockOwner = 'Session', @LockTimeout = 0;";
            var result = command.Parameters.Add("@result", System.Data.SqlDbType.Int);
            result.Direction = System.Data.ParameterDirection.ReturnValue;
            command.Parameters.AddWithValue("@resource", "LogicFit:CentralDatabaseBackup");
            await command.ExecuteNonQueryAsync(cancellationToken);
            if (result.Value is int value && value < 0)
                throw new InvalidOperationException("A backup batch is already running on another instance.");
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    private static async Task ReleaseDistributedLockAsync(SqlConnection? connection)
    {
        if (connection is null) return;
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "EXEC sp_releaseapplock @Resource = @resource, @LockOwner = 'Session';";
            command.Parameters.AddWithValue("@resource", "LogicFit:CentralDatabaseBackup");
            await command.ExecuteNonQueryAsync();
        }
        catch { /* the session release is best-effort during shutdown */ }
        await connection.DisposeAsync();
    }

    private async Task<bool> IsDuplicateIdempotencyAsync(string key, CancellationToken cancellationToken)
        => await db.BackupBatches.AsNoTracking().AnyAsync(x => x.IdempotencyKey == key, cancellationToken);

    private async Task WriteManifestAsync(string path, BackupBatch batch, CancellationToken cancellationToken)
    {
        var manifest = new
        {
            batch.Id,
            batch.Scope,
            batch.Status,
            batch.StartedAtUtc,
            batch.CompletedAtUtc,
            Artifacts = batch.Artifacts.Select(x => new
            {
                x.Id,
                x.TenantId,
                x.DatabaseResourceId,
                x.Status,
                x.StorageKey,
                x.SizeBytes,
                x.Sha256,
                x.StartedAtUtc,
                x.CompletedAtUtc,
                ErrorCode = x.ErrorMessage
            })
        };
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, manifest, cancellationToken: cancellationToken);
    }

    private bool TryGetSettings(out BackupSettings settings, out string reason)
    {
        settings = default!;
        reason = string.Empty;
        if (!configuration.GetValue("Backup:Enabled", false))
        {
            reason = "Backup is disabled on the server.";
            return false;
        }
        var configured = configuration["Backup:StorageDirectory"]?.Trim();
        if (string.IsNullOrWhiteSpace(configured) || Path.IsPathRooted(configured))
        {
            reason = "Backup:StorageDirectory must be a relative path inside App_Data.";
            return false;
        }
        var root = string.IsNullOrWhiteSpace(environment.ContentRootPath) ? AppContext.BaseDirectory : environment.ContentRootPath;
        var appData = Path.GetFullPath(Path.Combine(root, "App_Data"));
        var storage = Path.GetFullPath(Path.Combine(root, configured));
        var prefix = appData.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!storage.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            reason = "Backup storage must remain private inside App_Data.";
            return false;
        }
        settings = new BackupSettings(storage,
            Math.Clamp(configuration.GetValue("Backup:RetentionDays", 7), 1, 3650), GetRunAtUtc());
        return true;
    }

    private TimeSpan GetRunAtUtc() => TimeSpan.TryParse(configuration["Backup:RunAtUtc"], out var configured)
        ? configured : new TimeSpan(2, 0, 0);

    private static async Task<string> ComputeSha256Async(string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hash);
    }

    private static void PruneExpiredBackups(string directory, int retentionDays)
    {
        if (!Directory.Exists(directory)) return;
        foreach (var file in Directory.EnumerateFiles(directory)
                     .Select(path => new FileInfo(path))
                     .Where(file => file.CreationTimeUtc < DateTime.UtcNow.AddDays(-retentionDays)))
            TryDelete(file.FullName);
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { }
    }

    private static string Sanitize(string value) => string.Concat(value.Select(ch =>
        char.IsLetterOrDigit(ch) || ch is '-' or '_' ? ch : '_'));

    private static bool IsSafeBackupFileName(string fileName) =>
        !string.IsNullOrWhiteSpace(fileName) &&
        string.Equals(fileName, Path.GetFileName(fileName), StringComparison.Ordinal) &&
        BackupFileNamePattern.IsMatch(fileName);

    private async Task<BackupBatchDto> ToDtoAsync(BackupBatch batch, CancellationToken cancellationToken)
    {
        var tenantIds = batch.Artifacts.Where(x => x.TenantId.HasValue)
            .Select(x => x.TenantId!.Value)
            .Distinct()
            .ToArray();
        var metadata = tenantIds.Length == 0
            ? new Dictionary<Guid, BackupTenantMetadata>()
            : await db.Tenants.AsNoTracking().IgnoreQueryFilters()
                .Where(x => tenantIds.Contains(x.Id))
                .Select(x => new BackupTenantMetadata(x.Id, x.Name, x.Subdomain, x.WorkspaceType.ToString()))
                .ToDictionaryAsync(x => x.TenantId, cancellationToken);
        return ToDto(batch, metadata);
    }

    private static BackupBatchDto ToDto(
        BackupBatch batch,
        IReadOnlyDictionary<Guid, BackupTenantMetadata> metadata) => new(
        batch.Id,
        batch.Scope,
        batch.Status.ToString(),
        ToOffset(batch.StartedAtUtc),
        ToOffset(batch.CompletedAtUtc),
        batch.ManifestStorageKey,
        batch.Artifacts.OrderBy(x => x.TenantId).Select(x => new BackupArtifactDto(
            x.Id,
            x.TenantId,
            x.Status.ToString(),
            x.SizeBytes,
            ToOffset(x.StartedAtUtc),
            ToOffset(x.CompletedAtUtc),
            x.StorageKey,
            x.Sha256,
            x.ErrorMessage,
            x.TenantId is { } tenantId && metadata.TryGetValue(tenantId, out var tenant)
                ? tenant.Name
                : null,
            x.TenantId is { } identifierTenantId && metadata.TryGetValue(identifierTenantId, out var identifier)
                ? identifier.Subdomain
                : null,
            x.TenantId is { } typeTenantId && metadata.TryGetValue(typeTenantId, out var type)
                ? type.WorkspaceType
                : null)).ToList());

    private static DateTimeOffset? ToOffset(DateTime? value) =>
        value.HasValue ? new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)) : null;

    private sealed record BackupTarget(Guid? TenantId, Guid? DatabaseResourceId, string DatabaseName, string ConnectionString);
    private sealed record BackupTenantMetadata(Guid TenantId, string Name, string? Subdomain, string WorkspaceType);
    private sealed record BackupSettings(string StorageDirectory, int RetentionDays, TimeSpan RunAtUtc);
    private sealed record ExportResult(Guid ArtifactId, DatabaseBackupStatus Status, string? StorageKey, long SizeBytes,
        string? Sha256, DateTime? StartedAtUtc, DateTime? CompletedAtUtc, string? ErrorMessage);
}
