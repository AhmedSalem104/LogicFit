using LogicFit.Application.Common.Interfaces;
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
/// Development/CI-only BACPAC import provider. It requires pre-created pool resources and never
/// creates or deletes a database through an external hosting API.
/// </summary>
public sealed class LocalSqlDatabaseRestoreProvider(
    PlatformDbContext db,
    IBackupService backupService,
    IConnectionStringProtector protector,
    IConfiguration configuration,
    IHostEnvironment environment,
    IDateTimeService clock,
    ILogger<LocalSqlDatabaseRestoreProvider> logger) : IDatabaseRestoreProvider
{
    public DatabaseRestoreCapabilities GetCapabilities()
    {
        var enabled = environment.IsDevelopment() && configuration.GetValue("Restore:Enabled", false) &&
            string.Equals(configuration["Restore:Provider"], "LocalSql", StringComparison.OrdinalIgnoreCase);
        return new DatabaseRestoreCapabilities(enabled, enabled ? "LocalSql" : "Disabled", enabled, enabled,
            enabled ? null : "LocalSql restore is enabled only in Development with Restore:Enabled=true.");
    }

    public async Task<DatabaseRestoreResult> RestoreAsync(DatabaseRestoreRequest request, CancellationToken cancellationToken = default)
    {
        var capabilities = GetCapabilities();
        if (!capabilities.Enabled)
            return new DatabaseRestoreResult(false, null, null, "RESTORE_DISABLED", "LocalSql");

        var artifact = await db.DatabaseBackups.AsNoTracking().SingleOrDefaultAsync(x =>
            x.Id == request.SourceDatabaseBackupId && x.TenantId == request.TenantId &&
            x.Status == DatabaseBackupStatus.Completed && !string.IsNullOrWhiteSpace(x.StorageKey), cancellationToken);
        if (artifact is null)
            return new DatabaseRestoreResult(false, null, null, "SOURCE_BACKUP_NOT_FOUND", "LocalSql");

        var target = await db.DatabaseResources
            .Where(x => x.Status == DatabaseResourceStatus.Available &&
                (request.TargetDatabaseResourceId == null || x.Id == request.TargetDatabaseResourceId.Value))
            .OrderBy(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (target is null || string.IsNullOrWhiteSpace(target.EncryptedConnectionString))
            return new DatabaseRestoreResult(false, null, null, "RESTORE_TARGET_UNAVAILABLE", "LocalSql");

        var mapping = await db.TenantDatabaseMappings
            .SingleOrDefaultAsync(x => x.TenantId == request.TenantId && x.IsActive, cancellationToken);
        var previousMappingId = mapping?.Id;
        var tempDirectory = Path.Combine(AppContext.BaseDirectory, "App_Data", "restore-temp");
        Directory.CreateDirectory(tempDirectory);
        var tempPath = Path.Combine(tempDirectory, $"restore-{Guid.NewGuid():N}.bacpac");

        try
        {
            target.Status = DatabaseResourceStatus.RestorePending;
            target.ReservedForTenantId = request.TenantId;
            target.ReservedAtUtc = clock.UtcNow;
            await db.SaveChangesAsync(cancellationToken);

            var source = backupService.OpenRead(artifact.StorageKey!);
            await using (source.Content)
            await using (var file = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 64 * 1024, useAsync: true))
                await source.Content.CopyToAsync(file, cancellationToken);

            var connectionString = protector.Unprotect(target.EncryptedConnectionString);
            var builder = new SqlConnectionStringBuilder(connectionString) { InitialCatalog = "master" };
            await Task.Run(() =>
            {
                var dac = new DacServices(builder.ConnectionString);
                using var package = BacPackage.Load(tempPath);
                dac.ImportBacpac(package, target.DatabaseName);
            }, cancellationToken);

            var healthBuilder = new SqlConnectionStringBuilder(connectionString) { InitialCatalog = target.DatabaseName };
            await using (var connection = new SqlConnection(healthBuilder.ConnectionString))
                await connection.OpenAsync(cancellationToken);

            if (mapping is null)
            {
                db.TenantDatabaseMappings.Add(new Domain.Entities.TenantDatabaseMapping
                {
                    TenantId = request.TenantId,
                    DatabaseResourceId = target.Id,
                    Provider = "LocalSql",
                    EncryptedConnectionString = target.EncryptedConnectionString,
                    SchemaVersion = "BACPAC",
                    LastValidatedAtUtc = clock.UtcNow,
                    IsActive = true
                });
            }
            else
            {
                mapping.DatabaseResourceId = target.Id;
                mapping.Provider = "LocalSql";
                mapping.EncryptedConnectionString = target.EncryptedConnectionString;
                mapping.SchemaVersion = "BACPAC";
                mapping.LastValidatedAtUtc = clock.UtcNow;
            }

            target.Status = DatabaseResourceStatus.Assigned;
            target.AssignedAtUtc = clock.UtcNow;
            target.LastHealthCheckAtUtc = clock.UtcNow;
            target.LastError = null;
            await db.SaveChangesAsync(cancellationToken);
            return new DatabaseRestoreResult(true, target.Id, previousMappingId, null, "LocalSql", "BACPAC");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            target.Status = DatabaseResourceStatus.Faulted;
            target.LastError = "RESTORE_FAILED";
            await db.SaveChangesAsync(cancellationToken);
            logger.LogError(exception, "Local restore failed for tenant {TenantId} and resource {ResourceId}.", request.TenantId, target.Id);
            return new DatabaseRestoreResult(false, target.Id, previousMappingId, "RESTORE_FAILED", "LocalSql");
        }
        finally
        {
            try { if (File.Exists(tempPath)) File.Delete(tempPath); }
            catch (IOException) { logger.LogWarning("A temporary restore artifact could not be removed."); }
        }
    }
}
