using System.Security.Cryptography;
using System.Text;
using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Common.Services;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LogicFit.Infrastructure.Services;

public sealed class TenantBackupExportService(
    IApplicationDbContext context,
    IBackupService backupService,
    ISensitiveActionGrantService sensitiveActionGrantService,
    ICurrentUserService currentUser,
    IDateTimeService clock,
    IConfiguration configuration,
    ILogger<TenantBackupExportService> logger) : ITenantBackupExportService
{
    private const string DownloadGrantScope = "tenant-backup-download";

    public Task<SensitiveActionGrantDto> ReauthenticateAsync(
        Guid userId,
        Guid tenantId,
        string currentPassword,
        CancellationToken cancellationToken = default)
        => sensitiveActionGrantService.ReauthenticateAsync(
            userId, tenantId, currentPassword, SensitiveActionScopes.TenantBackupExport, cancellationToken);

    public Task<SensitiveActionGrantDto> ReauthenticateForDownloadAsync(
        Guid userId,
        Guid tenantId,
        string currentPassword,
        CancellationToken cancellationToken = default)
        => sensitiveActionGrantService.ReauthenticateAsync(
            userId, tenantId, currentPassword, SensitiveActionScopes.TenantBackupDownload, cancellationToken);

    public async Task<TenantBackupExportDto> CreateAsync(
        Guid userId,
        Guid tenantId,
        TenantBackupExportRequest request,
        CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty || userId == Guid.Empty)
            throw new UnauthorizedException("An authenticated workspace user is required.");

        await sensitiveActionGrantService.ConsumeAsync(
            request.GrantToken, userId, tenantId, SensitiveActionScopes.TenantBackupExport, cancellationToken);

        var idempotencyKey = string.IsNullOrWhiteSpace(request.IdempotencyKey)
            ? $"tenant-export:{tenantId:N}:{Guid.NewGuid():N}"
            : request.IdempotencyKey.Trim();
        if (idempotencyKey.Length > 200)
            throw new ValidationException("IdempotencyKey", "The idempotency key is too long.");

        var existing = await context.TenantBackupExports.AsNoTracking()
            .SingleOrDefaultAsync(x => x.TenantId == tenantId && x.IdempotencyKey == idempotencyKey, cancellationToken);
        if (existing is not null)
            return await ToDtoAsync(existing, cancellationToken);

        var now = clock.UtcNow;
        var dailyLimit = Math.Clamp(configuration.GetValue("Backup:TenantExportDailyLimit", 3), 1, 100);
        var todayCount = await context.TenantBackupExports.CountAsync(x => x.TenantId == tenantId && x.CreatedAt >= now.Date, cancellationToken);
        if (todayCount >= dailyLimit)
            throw new ConflictException("TENANT_BACKUP_DAILY_LIMIT_REACHED");

        var busy = await context.TenantBackupExports.AnyAsync(x => x.TenantId == tenantId &&
            (x.Status == TenantBackupExportStatus.Pending || x.Status == TenantBackupExportStatus.Running), cancellationToken);
        if (busy)
            throw new ConflictException("TENANT_BACKUP_EXPORT_IN_PROGRESS");

        var export = new TenantBackupExport
        {
            TenantId = tenantId,
            RequestedByUserId = userId,
            IdempotencyKey = idempotencyKey,
            Status = TenantBackupExportStatus.Running,
            StartedAtUtc = now
        };
        context.TenantBackupExports.Add(export);
        await context.SaveChangesAsync(cancellationToken);

        try
        {
            var batch = await backupService.CreateBatchAsync(
                new BackupBatchRequest(BackupScope.SelectedTenants, [tenantId], $"tenant-export:{export.Id:N}"),
                cancellationToken);
            var artifact = batch.Artifacts.FirstOrDefault(x => x.TenantId == tenantId &&
                x.Status == nameof(DatabaseBackupStatus.Completed) && !string.IsNullOrWhiteSpace(x.StorageKey));
            if (artifact is null)
                throw new InvalidOperationException("The workspace database export did not complete.");

            export.BackupBatchId = batch.Id;
            export.DatabaseBackupId = artifact.Id;
            export.Status = TenantBackupExportStatus.Completed;
            export.CompletedAtUtc = clock.UtcNow;
            SecurityAuditLog.Add(context, currentUser, clock, "TenantBackupExportCreated", true, userId, tenantId);
            await context.SaveChangesAsync(cancellationToken);
            return await ToDtoAsync(export, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            export.Status = TenantBackupExportStatus.Failed;
            export.CompletedAtUtc = clock.UtcNow;
            export.ErrorMessage = "TENANT_BACKUP_EXPORT_FAILED";
            logger.LogError(exception, "Tenant backup export {ExportId} failed for tenant {TenantId}.", export.Id, tenantId);
            SecurityAuditLog.Add(context, currentUser, clock, "TenantBackupExportFailed", false, userId, tenantId);
            await context.SaveChangesAsync(cancellationToken);
            return await ToDtoAsync(export, cancellationToken);
        }
    }

    public async Task<IReadOnlyList<TenantBackupExportDto>> ListAsync(
        Guid userId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var exports = await context.TenantBackupExports.AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(25)
            .ToListAsync(cancellationToken);
        return await Task.WhenAll(exports.Select(x => ToDtoAsync(x, cancellationToken)));
    }

    public async Task<TenantBackupExportDto> GetAsync(
        Guid userId,
        Guid tenantId,
        Guid exportId,
        CancellationToken cancellationToken = default)
    {
        var export = await context.TenantBackupExports.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == exportId && x.TenantId == tenantId, cancellationToken)
            ?? throw new NotFoundException("Tenant backup export", exportId);
        return await ToDtoAsync(export, cancellationToken);
    }

    public async Task<TenantBackupDownloadGrantDto> CreateDownloadGrantAsync(
        Guid userId,
        Guid tenantId,
        Guid exportId,
        string grantToken,
        CancellationToken cancellationToken = default)
    {
        await sensitiveActionGrantService.ConsumeAsync(
            grantToken, userId, tenantId, SensitiveActionScopes.TenantBackupDownload, cancellationToken);
        var export = await context.TenantBackupExports
            .SingleOrDefaultAsync(x => x.Id == exportId && x.TenantId == tenantId, cancellationToken)
            ?? throw new NotFoundException("Tenant backup export", exportId);
        if (export.Status != TenantBackupExportStatus.Completed || !export.DatabaseBackupId.HasValue)
            throw new ConflictException("TENANT_BACKUP_EXPORT_NOT_READY");

        var artifact = await context.DatabaseBackups.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == export.DatabaseBackupId.Value && x.TenantId == tenantId &&
                x.Status == DatabaseBackupStatus.Completed && !string.IsNullOrWhiteSpace(x.StorageKey), cancellationToken)
            ?? throw new ConflictException("TENANT_BACKUP_EXPORT_NOT_READY");

        var now = clock.UtcNow;
        var lifetimeMinutes = Math.Clamp(configuration.GetValue("Backup:TenantDownloadGrantMinutes", 5), 1, 15);
        var expiresAt = now.AddMinutes(lifetimeMinutes);
        var active = await context.TenantBackupDownloadGrants
            .Where(x => x.TenantBackupExportId == exportId && x.UserId == userId &&
                x.ConsumedAtUtc == null && x.RevokedAtUtc == null && x.ExpiresAtUtc > now)
            .ToListAsync(cancellationToken);
        foreach (var grant in active)
            grant.RevokedAtUtc = now;

        var rawToken = Base64UrlToken();
        context.TenantBackupDownloadGrants.Add(new TenantBackupDownloadGrant
        {
            TenantBackupExportId = exportId,
            TenantId = tenantId,
            UserId = userId,
            TokenHash = Hash(rawToken),
            ExpiresAtUtc = expiresAt,
            CreatedByIp = currentUser.IpAddress
        });
        SecurityAuditLog.Add(context, currentUser, clock, "TenantBackupDownloadGrantCreated", true, userId, tenantId);
        await context.SaveChangesAsync(cancellationToken);
        _ = artifact; // Metadata is intentionally not returned; the server streams it privately.
        return new TenantBackupDownloadGrantDto(
            exportId,
            rawToken,
            expiresAt,
            $"/api/tenant/backups/exports/{exportId:D}/download");
    }

    public async Task<TenantBackupDownload> OpenDownloadAsync(
        Guid userId,
        Guid tenantId,
        Guid exportId,
        string downloadToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(downloadToken))
            throw new UnauthorizedException("A valid download grant is required.");

        var now = clock.UtcNow;
        var hash = Hash(downloadToken);
        var grant = await context.TenantBackupDownloadGrants
            .SingleOrDefaultAsync(x => x.TenantBackupExportId == exportId && x.TenantId == tenantId &&
                x.UserId == userId && x.TokenHash == hash && x.ConsumedAtUtc == null &&
                x.RevokedAtUtc == null && x.ExpiresAtUtc > now, cancellationToken)
            ?? throw new UnauthorizedException("The download grant is invalid or expired.");

        var export = await context.TenantBackupExports.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == exportId && x.TenantId == tenantId &&
                x.Status == TenantBackupExportStatus.Completed && x.DatabaseBackupId.HasValue, cancellationToken)
            ?? throw new NotFoundException("Tenant backup export", exportId);
        var artifact = await context.DatabaseBackups.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == export.DatabaseBackupId!.Value && x.TenantId == tenantId &&
                x.Status == DatabaseBackupStatus.Completed && !string.IsNullOrWhiteSpace(x.StorageKey), cancellationToken)
            ?? throw new NotFoundException("Tenant backup artifact", exportId);

        BackupDownload download;
        try
        {
            download = backupService.OpenRead(artifact.StorageKey!);
        }
        catch (FileNotFoundException)
        {
            throw new NotFoundException("Tenant backup artifact", exportId);
        }

        await using var transaction = await context.BeginTransactionAsync(cancellationToken);
        grant.ConsumedAtUtc = now;
        grant.ConsumedByIp = currentUser.IpAddress;
        export = await context.TenantBackupExports.SingleAsync(x => x.Id == exportId, cancellationToken);
        export.DownloadedAtUtc = now;
        SecurityAuditLog.Add(context, currentUser, clock, "TenantBackupDownloaded", true, userId, tenantId);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            await download.Content.DisposeAsync();
            throw new UnauthorizedException("The download grant has already been consumed.");
        }

        return new TenantBackupDownload($"workspace-{tenantId:N}-{exportId:N}.bacpac", download.SizeBytes, download.Content);
    }

    private async Task<TenantBackupExportDto> ToDtoAsync(TenantBackupExport export, CancellationToken cancellationToken)
    {
        long? size = null;
        string? sha = null;
        if (export.DatabaseBackupId is Guid artifactId)
        {
            var artifact = await context.DatabaseBackups.AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == artifactId && x.TenantId == export.TenantId, cancellationToken);
            size = artifact?.SizeBytes;
            sha = artifact?.Sha256;
        }
        return new TenantBackupExportDto(export.Id, export.Status, export.CreatedAt, export.StartedAtUtc,
            export.CompletedAtUtc, export.DownloadedAtUtc, size, sha,
            export.Status == TenantBackupExportStatus.Failed ? "TENANT_BACKUP_EXPORT_FAILED" : null);
    }

    private static string Base64UrlToken()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');

    private static string Hash(string rawToken)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
}
