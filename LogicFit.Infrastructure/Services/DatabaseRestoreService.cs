using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Common.Services;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LogicFit.Infrastructure.Services;

public sealed class DatabaseRestoreService(
    IApplicationDbContext context,
    IBackupService backupService,
    ISensitiveActionGrantService sensitiveActionGrantService,
    IDatabaseRestoreProvider provider,
    ICurrentUserService currentUser,
    IDateTimeService clock,
    ILogger<DatabaseRestoreService> logger) : IDatabaseRestoreService
{
    private const string Scope = "platform-database-restore";

    public DatabaseRestoreCapabilities GetCapabilities() => provider.GetCapabilities();

    public Task<SensitiveActionGrantDto> ReauthenticateAsync(Guid userId, string currentPassword, CancellationToken cancellationToken = default)
        => sensitiveActionGrantService.ReauthenticateAsync(userId, null, currentPassword, Scope, cancellationToken);

    public async Task<RestoreJobDto> RestoreAsync(
        Guid userId,
        string grantToken,
        DatabaseRestoreRequest request,
        CancellationToken cancellationToken = default)
    {
        await sensitiveActionGrantService.ConsumeAsync(grantToken, userId, null, Scope, cancellationToken);
        var capabilities = provider.GetCapabilities();
        if (!capabilities.Enabled)
            throw new ConflictException(capabilities.Mode == "ManualOnly" ? "RESTORE_MANUAL_ONLY" : "RESTORE_DISABLED");
        if (string.IsNullOrWhiteSpace(request.Reason) || string.IsNullOrWhiteSpace(request.WorkspaceNameConfirmation))
            throw new ValidationException("Reason and workspace confirmation are required.");

        var tenant = await context.Tenants.IgnoreQueryFilters()
            .SingleOrDefaultAsync(x => x.Id == request.TenantId && !x.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Tenant", request.TenantId);
        if (!string.Equals(tenant.Name.Trim(), request.WorkspaceNameConfirmation.Trim(), StringComparison.Ordinal))
            throw new ValidationException("WorkspaceNameConfirmation", "Workspace confirmation does not match.");

        var active = await context.RestoreJobs.AnyAsync(x => x.TenantId == request.TenantId &&
            (x.Status == RestoreJobStatus.Pending || x.Status == RestoreJobStatus.Running), cancellationToken);
        if (active) throw new ConflictException("RESTORE_ALREADY_RUNNING");

        var job = new RestoreJob
        {
            TenantId = request.TenantId,
            RequestedByUserId = userId,
            SourceDatabaseBackupId = request.SourceDatabaseBackupId,
            TargetDatabaseResourceId = request.TargetDatabaseResourceId,
            Status = RestoreJobStatus.Running,
            Provider = capabilities.Mode,
            Reason = request.Reason.Trim(),
            WorkspaceNameConfirmation = request.WorkspaceNameConfirmation.Trim(),
            StartedAtUtc = clock.UtcNow
        };
        context.RestoreJobs.Add(job);
        await context.SaveChangesAsync(cancellationToken);

        try
        {
            // A fresh tenant export is the mandatory pre-restore safety point. The provider then
            // imports into a separate pool resource and switches the central mapping only after
            // connectivity/schema smoke checks succeed.
            var preBackup = await backupService.CreateBatchAsync(
                new BackupBatchRequest(BackupScope.SelectedTenants, [request.TenantId], $"restore-prebackup:{job.Id:N}"), cancellationToken);
            if (preBackup.Artifacts.All(x => x.Status != nameof(DatabaseBackupStatus.Completed)))
                throw new InvalidOperationException("Pre-restore backup did not complete.");

            var result = await provider.RestoreAsync(request, cancellationToken);
            job.Status = result.Succeeded ? RestoreJobStatus.Completed : RestoreJobStatus.Failed;
            job.CompletedAtUtc = clock.UtcNow;
            job.ErrorCode = result.ErrorCode;
            job.TargetDatabaseResourceId = result.TargetDatabaseResourceId;
            job.PreviousMappingId = result.PreviousMappingId;
            SecurityAuditLog.Add(context, currentUser, clock, result.Succeeded ? "TenantRestoreCompleted" : "TenantRestoreFailed", result.Succeeded, userId, request.TenantId);
            await context.SaveChangesAsync(cancellationToken);
            return ToDto(job);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            job.Status = RestoreJobStatus.Failed;
            job.CompletedAtUtc = clock.UtcNow;
            job.ErrorCode = "RESTORE_FAILED";
            logger.LogError(exception, "Restore job {RestoreJobId} failed for tenant {TenantId}.", job.Id, request.TenantId);
            SecurityAuditLog.Add(context, currentUser, clock, "TenantRestoreFailed", false, userId, request.TenantId);
            await context.SaveChangesAsync(cancellationToken);
            return ToDto(job);
        }
    }

    public async Task<IReadOnlyList<RestoreJobDto>> ListAsync(CancellationToken cancellationToken = default)
        => await context.RestoreJobs.AsNoTracking().OrderByDescending(x => x.CreatedAt).Take(50)
            .Select(x => new RestoreJobDto(x.Id, x.TenantId, x.Status, x.Provider, x.CreatedAt,
                x.StartedAtUtc, x.CompletedAtUtc, x.ErrorCode)).ToListAsync(cancellationToken);

    private static RestoreJobDto ToDto(RestoreJob job)
        => new(job.Id, job.TenantId, job.Status, job.Provider, job.CreatedAt, job.StartedAtUtc, job.CompletedAtUtc, job.ErrorCode);
}
