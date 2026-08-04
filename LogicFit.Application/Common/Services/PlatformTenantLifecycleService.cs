using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Identity;
using LogicFit.Application.Features.Platform.Tenants.DTOs;
using LogicFit.Domain.Authorization;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LogicFit.Application.Common.Services;

/// <summary>
/// Platform-only owner-account and workspace lifecycle operations. The service deliberately keeps
/// Global Identity rows intact when a workspace is removed; only workspace associations are
/// revoked/deleted. Permanent deletion is gated by a completed tenant backup and a provider that
/// explicitly advertises database purge capability.
/// </summary>
public sealed class PlatformTenantLifecycleService(
    IApplicationDbContext context,
    IBackupService backupService,
    ITenantDatabasePurgeProvider purgeProvider,
    IDatabaseResourcePool resourcePool,
    IdentityEmailActionService emailActionService,
    ICurrentUserService currentUser,
    IDateTimeService clock,
    ILogger<PlatformTenantLifecycleService> logger) : IPlatformTenantLifecycleService
{
    private const int PasswordResetExpiryMinutes = 30;

    public async Task<PlatformTenantCredentialsDto> GetCredentialsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var tenant = await GetTenantAsync(tenantId, cancellationToken);
        var owner = await FindOwnerAsync(tenantId, cancellationToken);

        // This is an intentional security event for a read operation. No password, hash, token,
        // or connection material is included in the audit record or response.
        SecurityAuditLog.Add(context, currentUser, clock, "PlatformTenantCredentialsViewed", true, tenantId, tenantId);
        await context.SaveChangesAsync(cancellationToken);

        return new PlatformTenantCredentialsDto(
            tenant.Id,
            tenant.Name,
            owner?.Identity?.Email ?? owner?.User?.Email,
            owner?.Identity is not null,
            owner?.Identity?.IsActive ?? false,
            owner?.Identity?.EmailVerifiedAt,
            owner?.User?.IsActive ?? false,
            owner?.Membership?.Status,
            owner?.Identity?.LastLoginAt,
            owner?.Identity?.LockoutEndUtc,
            owner is not null &&
            (owner.Identity is null
                ? owner.User?.IsActive == true
                : owner.Identity.IsActive && owner.Identity.EmailVerifiedAt.HasValue));
    }

    public async Task<PlatformTenantPasswordResetDto> RequestPasswordResetAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var tenant = await GetTenantAsync(tenantId, cancellationToken);
        if (tenant.IsDeleted || tenant.Status == TenantStatus.Deleted)
            throw new ConflictException("TENANT_IS_DELETED");

        // Fail before changing/linking a legacy account when email delivery is not configured.
        emailActionService.EnsureDeliveryAvailable();

        var owner = await FindOwnerAsync(tenantId, cancellationToken)
            ?? throw new NotFoundException("Gym owner", tenantId);
        var ownerEmail = owner.Identity?.Email ?? owner.User?.Email;
        if (string.IsNullOrWhiteSpace(ownerEmail))
            throw new ConflictException("OWNER_EMAIL_NOT_AVAILABLE");

        var identity = owner.Identity;
        if (identity is null)
        {
            // Legacy dashboard-created owners may predate Global Identity. Linking by the existing
            // email preserves the account and makes the normal identity-first reset flow usable;
            // it never copies or returns a plaintext password.
            var normalizedEmail = IdentityEmailAddress.Normalize(ownerEmail);
            identity = await context.IdentityAccounts
                .SingleOrDefaultAsync(x => x.NormalizedEmail == normalizedEmail, cancellationToken);

            if (identity is null)
            {
                identity = new IdentityAccount
                {
                    FullName = owner.User?.Email ?? ownerEmail,
                    Email = ownerEmail,
                    NormalizedEmail = normalizedEmail,
                    PasswordHash = owner.User?.PasswordHash ?? string.Empty,
                    IsActive = owner.User?.IsActive ?? true,
                    // An administrator is linking an already-provisioned owner account. The reset
                    // link still has to be delivered to this exact address before a new password
                    // can be selected.
                    EmailVerifiedAt = clock.UtcNow
                };
                context.IdentityAccounts.Add(identity);
            }

            if (owner.User is not null)
                owner.User.IdentityAccountId = identity.Id;

            if (owner.Membership is null && owner.User is not null)
            {
                context.WorkspaceMemberships.Add(new WorkspaceMembership
                {
                    TenantId = tenantId,
                    IdentityAccountId = identity.Id,
                    UserId = owner.User.Id,
                    Role = owner.User.Role,
                    Status = tenant.Status == TenantStatus.Active
                        ? WorkspaceMembershipStatus.Active
                        : WorkspaceMembershipStatus.PendingPlatformApproval
                });
            }

            await context.SaveChangesAsync(cancellationToken);
            owner = owner with { Identity = identity };
        }

        if (!identity.IsActive)
            throw new ConflictException("OWNER_IDENTITY_INACTIVE");
        if (identity.EmailVerifiedAt is null)
            throw new ConflictException("OWNER_IDENTITY_EMAIL_UNVERIFIED");

        try
        {
            await emailActionService.IssueAsync(identity, EmailActionTokenPurpose.PasswordReset, cancellationToken);
            SecurityAuditLog.Add(context, currentUser, clock, "PlatformTenantPasswordResetRequested", true, tenantId, tenantId);
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Owner password reset could not be issued for TenantId {TenantId}.", tenantId);
            SecurityAuditLog.Add(context, currentUser, clock, "PlatformTenantPasswordResetRequested", false, tenantId, tenantId);
            await context.SaveChangesAsync(cancellationToken);
            throw;
        }

        return new PlatformTenantPasswordResetDto(tenantId, identity.Email, true, PasswordResetExpiryMinutes);
    }

    public async Task<PlatformTenantDto> SoftDeleteAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var tenant = await GetTenantAsync(tenantId, cancellationToken);
        if (tenant.IsDeleted)
            return ToDto(tenant);

        tenant.IsDeleted = true;
        tenant.Status = TenantStatus.Deleted;
        tenant.DeletedAt = clock.UtcNow;
        tenant.DeletedBy = currentUser.UserId;
        tenant.SuspensionReason = SuspensionReason.None;
        await RevokeWorkspaceAccessAsync(tenantId, cancellationToken);

        SecurityAuditLog.Add(context, currentUser, clock, "PlatformTenantSoftDeleted", true, tenantId, tenantId);
        await context.SaveChangesAsync(cancellationToken);
        return ToDto(tenant);
    }

    public async Task<PlatformTenantDto> RestoreAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var tenant = await GetTenantAsync(tenantId, cancellationToken);
        if (!tenant.IsDeleted && tenant.Status != TenantStatus.Deleted)
            return ToDto(tenant);

        var hasDatabaseMapping = await context.TenantDatabaseMappings
            .IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.IsActive, cancellationToken);
        if (!hasDatabaseMapping)
            throw new ConflictException("TENANT_PERMANENTLY_DELETED");

        tenant.IsDeleted = false;
        tenant.Status = TenantStatus.Active;
        tenant.DeletedAt = null;
        tenant.DeletedBy = null;
        tenant.SuspensionReason = SuspensionReason.None;

        SecurityAuditLog.Add(context, currentUser, clock, "PlatformTenantRestored", true, tenantId, tenantId);
        await context.SaveChangesAsync(cancellationToken);
        return ToDto(tenant);
    }

    public async Task<PlatformTenantPermanentDeleteDto> PermanentlyDeleteAsync(
        Guid tenantId,
        PlatformTenantDeleteRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null || !request.PreserveGlobalIdentity)
            throw new ValidationException("PreserveGlobalIdentity", "Global Identity must remain preserved for this operation.");

        var tenant = await GetTenantAsync(tenantId, cancellationToken);
        if (string.IsNullOrWhiteSpace(request.TenantNameConfirmation) ||
            !string.Equals(tenant.Name.Trim(), request.TenantNameConfirmation.Trim(), StringComparison.Ordinal))
            throw new ValidationException("TenantNameConfirmation", "Type the exact gym name to confirm permanent deletion.");

        var capabilities = purgeProvider.GetCapabilities();
        if (!capabilities.Enabled)
        {
            SecurityAuditLog.Add(context, currentUser, clock, "PlatformTenantPermanentDeleteRequested", false, tenantId, tenantId);
            await context.SaveChangesAsync(cancellationToken);
            throw new ConflictException(capabilities.Mode == "ManualOnly"
                ? "TENANT_DATABASE_PURGE_MANUAL_ONLY"
                : "TENANT_DATABASE_PURGE_DISABLED");
        }

        var mapping = await context.TenantDatabaseMappings
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(x => x.TenantId == tenantId && x.IsActive, cancellationToken);
        if (mapping is null)
            throw new ConflictException("TENANT_DATABASE_MAPPING_NOT_FOUND");

        var resource = await context.DatabaseResources
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(x => x.Id == mapping.DatabaseResourceId &&
                x.ReservedForTenantId == tenantId, cancellationToken);
        if (resource is null)
            throw new ConflictException("TENANT_DATABASE_RESOURCE_NOT_FOUND");

        SecurityAuditLog.Add(context, currentUser, clock, "PlatformTenantPermanentDeleteRequested", true, tenantId, tenantId);
        await context.SaveChangesAsync(cancellationToken);

        BackupBatchDto backup;
        try
        {
            backup = await backupService.CreateBatchAsync(
                new BackupBatchRequest(
                    BackupScope.SelectedTenants,
                    [tenantId],
                    $"tenant-permanent-delete:{tenantId:N}:{Guid.NewGuid():N}"),
                cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Permanent delete backup failed for TenantId {TenantId}.", tenantId);
            SecurityAuditLog.Add(context, currentUser, clock, "PlatformTenantPermanentDeleteBackupFailed", false, tenantId, tenantId);
            await context.SaveChangesAsync(cancellationToken);
            throw new ConflictException("TENANT_PERMANENT_DELETE_BACKUP_FAILED");
        }

        var artifact = backup.Artifacts.FirstOrDefault(x => x.TenantId == tenantId &&
            x.Status == nameof(DatabaseBackupStatus.Completed) && !string.IsNullOrWhiteSpace(x.StorageKey));
        if (artifact is null)
        {
            SecurityAuditLog.Add(context, currentUser, clock, "PlatformTenantPermanentDeleteBackupFailed", false, tenantId, tenantId);
            await context.SaveChangesAsync(cancellationToken);
            throw new ConflictException("TENANT_PERMANENT_DELETE_BACKUP_FAILED");
        }

        SecurityAuditLog.Add(context, currentUser, clock, "PlatformTenantPermanentDeleteBackupCompleted", true, tenantId, tenantId);
        var wasAlreadySoftDeleted = tenant.IsDeleted;
        var previousStatus = tenant.Status;
        if (!wasAlreadySoftDeleted)
            tenant.Status = TenantStatus.Archived;
        await RevokeWorkspaceAccessAsync(tenantId, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        SecurityAuditLog.Add(context, currentUser, clock, "PlatformTenantPermanentDeletePurgeStarted", true, tenantId, tenantId);
        await context.SaveChangesAsync(cancellationToken);
        var purge = await purgeProvider.PurgeAsync(
            new TenantDatabasePurgeRequest(tenantId, resource.Id, mapping.Provider),
            cancellationToken);
        if (!purge.Succeeded)
        {
            tenant.Status = wasAlreadySoftDeleted ? TenantStatus.Deleted : previousStatus;
            SecurityAuditLog.Add(context, currentUser, clock, "PlatformTenantPermanentDeletePurgeFailed", false, tenantId, tenantId);
            await context.SaveChangesAsync(cancellationToken);
            throw new ConflictException(purge.ErrorCode ?? "TENANT_DATABASE_PURGE_FAILED");
        }

        SecurityAuditLog.Add(context, currentUser, clock, "PlatformTenantPermanentDeletePurgeCompleted", true, tenantId, tenantId);

        // The external purge succeeded. The remaining mutations are Platform DB tombstone and
        // association cleanup. The owner IdentityAccount is deliberately never deleted.
        mapping.IsActive = false;
        mapping.EncryptedConnectionString = "PURGED";
        tenant.IsDeleted = true;
        tenant.Status = TenantStatus.Deleted;
        tenant.DeletedAt = clock.UtcNow;
        tenant.DeletedBy = currentUser.UserId;

        var memberships = await context.WorkspaceMemberships
            .IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && !x.IsDeleted)
            .ToListAsync(cancellationToken);
        foreach (var membership in memberships)
        {
            membership.Status = WorkspaceMembershipStatus.Revoked;
            membership.IsDeleted = true;
            membership.DeletedAt = clock.UtcNow;
            membership.DeletedBy = currentUser.UserId;
        }

        var invites = await context.WorkspaceInvites
            .IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.Status == WorkspaceInviteStatus.Pending)
            .ToListAsync(cancellationToken);
        foreach (var invite in invites)
        {
            invite.Status = WorkspaceInviteStatus.Revoked;
            invite.RevokedAt = clock.UtcNow;
        }

        var joinCodes = await context.WorkspaceClientJoinCodes
            .IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var joinCode in joinCodes)
            joinCode.RevokedAt = clock.UtcNow;

        var requests = await context.ApplicationRequests
            .IgnoreQueryFilters()
            .Where(x => (x.TargetWorkspaceId == tenantId || x.ProvisionedWorkspaceId == tenantId) &&
                x.Status != ApplicationRequestStatus.Cancelled &&
                x.Status != ApplicationRequestStatus.Rejected &&
                x.Status != ApplicationRequestStatus.Expired)
            .ToListAsync(cancellationToken);
        foreach (var application in requests)
        {
            application.Status = ApplicationRequestStatus.Cancelled;
            application.DecisionReason = "WORKSPACE_PERMANENTLY_DELETED";
            application.ReviewedAt = clock.UtcNow;
            application.ReviewedBy = currentUser.UserId;
        }

        var released = await resourcePool.ReleaseAsync(resource.Id, tenantId, cancellationToken);
        if (!released)
        {
            SecurityAuditLog.Add(context, currentUser, clock, "PlatformTenantPermanentDeleteResourceReleaseFailed", false, tenantId, tenantId);
            await context.SaveChangesAsync(cancellationToken);
            throw new ConflictException("TENANT_DATABASE_RESOURCE_RELEASE_FAILED");
        }

        SecurityAuditLog.Add(context, currentUser, clock, "PlatformTenantPermanentDeleteResourceReleased", true, tenantId, tenantId);
        SecurityAuditLog.Add(context, currentUser, clock, "PlatformTenantPermanentDeleteCompleted", true, tenantId, tenantId);
        await context.SaveChangesAsync(cancellationToken);

        return new PlatformTenantPermanentDeleteDto(
            tenantId,
            tenant.Name,
            nameof(TenantStatus.Deleted),
            backup.Id,
            artifact.Id,
            resource.Id,
            true);
    }

    private async Task<Tenant> GetTenantAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty || tenantId == PlatformConstants.PlatformTenantId)
            throw new ForbiddenException("The platform tenant cannot be modified.");

        return await context.Tenants
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(x => x.Id == tenantId, cancellationToken)
            ?? throw new NotFoundException(nameof(Tenant), tenantId);
    }

    private async Task RevokeWorkspaceAccessAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var identityIds = await context.WorkspaceMemberships
            .IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && !x.IsDeleted)
            .Select(x => x.IdentityAccountId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (identityIds.Count > 0)
        {
            var sessions = await context.IdentityWorkspaceSessions
                .IgnoreQueryFilters()
                .Where(x => identityIds.Contains(x.IdentityAccountId) && x.RevokedAt == null && x.ExpiresAt > now)
                .ToListAsync(cancellationToken);
            foreach (var session in sessions)
                session.RevokedAt = now;
        }

        var refreshTokens = await context.RefreshTokens
            .IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.RevokedAt == null && x.ExpiresAt > now)
            .ToListAsync(cancellationToken);
        foreach (var token in refreshTokens)
        {
            token.RevokedAt = now;
            token.RevokedByIp = currentUser.IpAddress;
        }

        var localUsers = await context.Users
            .IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && !x.IsDeleted)
            .ToListAsync(cancellationToken);
        foreach (var user in localUsers)
            user.PermissionsVersion++;
    }

    private async Task<OwnerContext?> FindOwnerAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var membership = await context.WorkspaceMemberships
            .IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && !x.IsDeleted &&
                (x.Role == UserRole.Owner || x.Role == UserRole.FreelanceOwner))
            .OrderByDescending(x => x.Status == WorkspaceMembershipStatus.Active)
            .FirstOrDefaultAsync(cancellationToken);

        var userId = membership?.UserId;
        var user = userId.HasValue
            ? await context.Users.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.Id == userId.Value, cancellationToken)
            : await context.Users.IgnoreQueryFilters()
                .Where(x => x.TenantId == tenantId && !x.IsDeleted &&
                    (x.Role == UserRole.Owner || x.Role == UserRole.FreelanceOwner))
                .OrderBy(x => x.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

        if (membership is null && user is null)
            return null;

        var identityId = membership?.IdentityAccountId ?? user?.IdentityAccountId;
        var identity = identityId.HasValue
            ? await context.IdentityAccounts.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.Id == identityId.Value, cancellationToken)
            : null;
        return new OwnerContext(user, membership, identity);
    }

    private static PlatformTenantDto ToDto(Tenant tenant)
        => new()
        {
            Id = tenant.Id,
            Name = tenant.Name,
            Subdomain = tenant.Subdomain,
            Status = tenant.Status,
            Email = tenant.Email,
            PhoneNumber = tenant.PhoneNumber,
            CreatedAt = tenant.CreatedAt,
            IsDeleted = tenant.IsDeleted,
            DeletedAt = tenant.DeletedAt
        };

    private sealed record OwnerContext(User? User, WorkspaceMembership? Membership, IdentityAccount? Identity);
}
