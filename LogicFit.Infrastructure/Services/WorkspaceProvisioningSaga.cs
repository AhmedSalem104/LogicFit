using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Authorization;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LogicFit.Infrastructure.Services;

/// <summary>
/// Coordinates Platform state around the provider's cross-database work. There is intentionally
/// no distributed transaction: the persistent job and idempotency key make retries safe.
/// </summary>
public sealed class WorkspaceProvisioningSaga(
    ApplicationDbContext db,
    IDatabaseProvisioningProvider provider,
    IDateTimeService clock,
    ILogger<WorkspaceProvisioningSaga> logger) : IWorkspaceProvisioningSaga
{
    public async Task<WorkspaceProvisioningOutcome> RunAsync(
        Guid applicationRequestId,
        CancellationToken cancellationToken = default)
    {
        var application = await db.ApplicationRequests
            .Include(x => x.IdentityAccount)
            .FirstOrDefaultAsync(x => x.Id == applicationRequestId, cancellationToken)
            ?? throw new InvalidOperationException("The workspace application was not found.");
        if (!application.ProvisionedWorkspaceId.HasValue)
            throw new InvalidOperationException("The application has no central workspace placeholder.");

        var tenantId = application.ProvisionedWorkspaceId.Value;
        var payment = await db.PaymentRequests
            .FirstOrDefaultAsync(x => x.ApplicationRequestId == applicationRequestId, cancellationToken);
        if (payment?.Status != PaymentRequestStatus.Approved)
            throw new InvalidOperationException("Payment approval is required before provisioning.");

        var job = await db.ProvisioningJobs
            .FirstOrDefaultAsync(x => x.ApplicationRequestId == applicationRequestId, cancellationToken);
        if (job?.Status == ProvisioningJobStatus.Completed)
            return new WorkspaceProvisioningOutcome(tenantId, applicationRequestId, job.Status, job.DatabaseResourceId);

        if (job is null)
        {
            job = new ProvisioningJob
            {
                TenantId = tenantId,
                ApplicationRequestId = applicationRequestId,
                IdempotencyKey = $"workspace-provisioning:{applicationRequestId}"
            };
            db.ProvisioningJobs.Add(job);
        }

        var now = clock.UtcNow;
        job.Status = ProvisioningJobStatus.Provisioning;
        job.AttemptCount++;
        job.StartedAtUtc ??= now;
        job.LastErrorCode = null;
        job.LastError = null;
        var tenant = await db.Tenants.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == tenantId, cancellationToken)
            ?? throw new InvalidOperationException("The workspace placeholder was not found.");
        tenant.Status = TenantStatus.Provisioning;
        await db.SaveChangesAsync(cancellationToken);

        DatabaseProvisioningResult result;
        try
        {
            result = await provider.ProvisionAsync(tenantId, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Provisioning provider failed for ApplicationRequestId {ApplicationRequestId}.", applicationRequestId);
            return await MarkFailedAsync(job, tenant, applicationRequestId, tenantId, "TENANT_PROVISIONING_FAILED", cancellationToken);
        }

        job.DatabaseResourceId = result.ResourceId;
        if (result.Status == "AwaitingDatabaseCapacity")
        {
            job.Status = ProvisioningJobStatus.AwaitingDatabaseCapacity;
            job.LastErrorCode = result.ErrorCode ?? "DATABASE_CAPACITY_UNAVAILABLE";
            job.NextAttemptAtUtc = now.AddHours(1);
            tenant.Status = TenantStatus.AwaitingDatabaseCapacity;
            await db.SaveChangesAsync(cancellationToken);
            return new WorkspaceProvisioningOutcome(tenantId, applicationRequestId, job.Status, result.ResourceId, job.LastErrorCode);
        }

        if (result.Status != "Completed")
            return await MarkFailedAsync(job, tenant, applicationRequestId, tenantId, result.ErrorCode ?? "TENANT_PROVISIONING_FAILED", cancellationToken);

        // ApplicationDbContext is still the compatibility host during the migration split. Keep
        // a scalar bridge row with the same id as the tenant-local owner so existing membership
        // foreign keys remain valid; the tenant database remains the operational source of truth.
        var localUserId = result.LocalUserId ?? throw new InvalidOperationException("Provider did not return the local owner id.");
        var mustChangePassword = application.PayloadJson.Contains("\"mustChangePassword\":true", StringComparison.OrdinalIgnoreCase);
        var compatibilityUser = await db.Set<User>().IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == localUserId, cancellationToken);
        if (compatibilityUser is null)
        {
            compatibilityUser = new User
            {
                Id = localUserId,
                TenantId = tenantId,
                IdentityAccountId = application.IdentityAccountId,
                Email = application.IdentityAccount.Email,
                PhoneNumber = application.IdentityAccount.PhoneNumber,
                PasswordHash = application.IdentityAccount.PasswordHash,
                Role = application.RequestedRole ?? UserRole.Owner,
                IsActive = true,
                MustChangePassword = mustChangePassword
            };
            db.Set<User>().Add(compatibilityUser);
            db.UserProfiles.Add(new UserProfile
            {
                UserId = localUserId,
                FullName = application.IdentityAccount.FullName
            });
        }
        else if (mustChangePassword)
        {
            compatibilityUser.MustChangePassword = true;
        }

        var membership = await db.WorkspaceMemberships.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.IdentityAccountId == application.IdentityAccountId, cancellationToken);
        if (membership is null)
        {
            membership = new WorkspaceMembership
            {
                TenantId = tenantId,
                IdentityAccountId = application.IdentityAccountId,
                UserId = localUserId,
                Role = application.RequestedRole ?? UserRole.Owner,
                Status = WorkspaceMembershipStatus.Active,
                ApprovedAt = now,
                ApprovedBy = "provisioning-saga"
            };
            db.WorkspaceMemberships.Add(membership);
        }
        membership.Status = WorkspaceMembershipStatus.Active;
        membership.UserId = localUserId;

        var roleName = membership.Role == UserRole.FreelanceOwner ? SystemRoles.FreelanceOwner : SystemRoles.Owner;
        var role = await db.AppRoles.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == null && x.Name == roleName && !x.IsDeleted, cancellationToken);
        if (role is not null && !await db.UserRoleAssignments.IgnoreQueryFilters()
                .AnyAsync(x => x.UserId == membership.UserId && x.RoleId == role.Id && x.TenantId == tenantId, cancellationToken))
        {
            db.UserRoleAssignments.Add(new UserRoleAssignment
            {
                UserId = membership.UserId,
                RoleId = role.Id,
                TenantId = tenantId
            });
        }

        var subscription = await db.TenantSubscriptions
            .FirstOrDefaultAsync(x => x.Id == payment.TenantSubscriptionId, cancellationToken)
            ?? throw new InvalidOperationException("The pending subscription was not found.");
        if (subscription.Status != TenantSubscriptionStatus.PendingActivation)
            throw new InvalidOperationException($"Subscription is {subscription.Status}, not PendingActivation.");
        var durationDays = subscription.PlanId == Guid.Empty
            ? 30
            : await db.Plans.Where(x => x.Id == subscription.PlanId).Select(x => x.DurationInDays).FirstOrDefaultAsync(cancellationToken);
        durationDays = durationDays <= 0 ? 30 : durationDays;
        subscription.Status = TenantSubscriptionStatus.Active;
        subscription.StartDate = now;
        subscription.EndDate = now.AddDays(durationDays);
        subscription.RenewDate = subscription.EndDate;
        subscription.ApprovedAt = now;
        tenant.Status = TenantStatus.Active;
        job.Status = ProvisioningJobStatus.Completed;
        job.CompletedAtUtc = now;
        db.OutboxMessages.Add(new OutboxMessage
        {
            Type = "workspace.provisioning.completed",
            Payload = $"{{\"applicationId\":\"{applicationRequestId}\",\"tenantId\":\"{tenantId}\"}}",
            OccurredAtUtc = now,
            IdempotencyKey = $"workspace-provisioning:{applicationRequestId}:completed"
        });
        await db.SaveChangesAsync(cancellationToken);
        return new WorkspaceProvisioningOutcome(tenantId, applicationRequestId, job.Status, result.ResourceId);
    }

    private async Task<WorkspaceProvisioningOutcome> MarkFailedAsync(
        ProvisioningJob job,
        Tenant tenant,
        Guid applicationRequestId,
        Guid tenantId,
        string errorCode,
        CancellationToken cancellationToken)
    {
        job.Status = ProvisioningJobStatus.Failed;
        job.LastErrorCode = errorCode;
        job.LastError = "Provisioning failed; Platform retry is required.";
        job.NextAttemptAtUtc = clock.UtcNow.AddMinutes(Math.Min(60, Math.Max(5, job.AttemptCount * 5)));
        tenant.Status = TenantStatus.ProvisioningFailed;
        await db.SaveChangesAsync(cancellationToken);
        return new WorkspaceProvisioningOutcome(tenantId, applicationRequestId, job.Status, job.DatabaseResourceId, errorCode);
    }
}
