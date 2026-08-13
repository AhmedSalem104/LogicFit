using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Identity.DTOs;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Identity;

/// <summary>Creates the short-lived workspace selection context after verified password authentication.</summary>
public sealed class IdentityWorkspaceSessionIssuer : IIdentityWorkspaceSessionIssuer
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeService _dateTimeService;
    private readonly ICurrentUserService _currentUserService;

    public IdentityWorkspaceSessionIssuer(IApplicationDbContext context, IDateTimeService dateTimeService, ICurrentUserService currentUserService)
        => (_context, _dateTimeService, _currentUserService) = (context, dateTimeService, currentUserService);

    public async Task<IdentitySignInDto> IssueAsync(Guid identityAccountId, CancellationToken cancellationToken = default)
    {
        var identity = await _context.IdentityAccounts.SingleOrDefaultAsync(x => x.Id == identityAccountId, cancellationToken)
            ?? throw new UnauthorizedException("Invalid credentials");
        var now = _dateTimeService.UtcNow;
        var memberships = await _context.WorkspaceMemberships.IgnoreQueryFilters()
            // Memberships and workspace metadata are platform-owned. The workspace User row is
            // tenant-owned and is intentionally not mapped by PlatformDbContext; including it
            // here made every successful identity login fail with a 500 after the platform/tenant
            // database split. Selection/access validation reads the local account at the point
            // where a tenant context is available.
            .Include(x => x.Tenant)
            .Where(x => x.IdentityAccountId == identity.Id && !x.IsDeleted &&
                (x.Status == WorkspaceMembershipStatus.Active ||
                    (x.Status == WorkspaceMembershipStatus.PendingPlatformApproval &&
                     x.Role == UserRole.Owner &&
                     x.Tenant.WorkspaceType == WorkspaceType.Gym &&
                     x.Tenant.Status == TenantStatus.Active)) &&
                !x.Tenant.IsDeleted)
            .OrderBy(x => x.Tenant.Name).ToListAsync(cancellationToken);

        // Older gyms could be activated before the approval handler also repaired the owner's
        // membership. An Active gym is the platform's approval decision, so reconcile only this
        // narrow, owner-only state here. Pending client/workspace approvals must remain blocked.
        foreach (var membership in memberships.Where(x => x.Status == WorkspaceMembershipStatus.PendingPlatformApproval))
        {
            membership.Status = WorkspaceMembershipStatus.Active;
            membership.ApprovedAt ??= now;
            membership.ApprovedBy ??= "identity-login-reconciliation";
        }

        var pendingApplications = await _context.ApplicationRequests
            .Where(x => x.IdentityAccountId == identity.Id && (x.Status == ApplicationRequestStatus.Draft ||
                x.Status == ApplicationRequestStatus.Submitted || x.Status == ApplicationRequestStatus.UnderReview ||
                x.Status == ApplicationRequestStatus.NeedsMoreInformation))
            .OrderByDescending(x => x.SubmittedAt).ToListAsync(cancellationToken);
        var pendingApplicationIds = pendingApplications.Select(x => x.Id).ToArray();
        var pendingWorkspaceIds = pendingApplications
            .Where(x => x.ProvisionedWorkspaceId.HasValue)
            .Select(x => x.ProvisionedWorkspaceId!.Value)
            .Distinct()
            .ToArray();
        var pendingPayments = await _context.PaymentRequests.AsNoTracking()
            .Where(x => x.ApplicationRequestId.HasValue && pendingApplicationIds.Contains(x.ApplicationRequestId.Value))
            .Select(x => new PendingPaymentSnapshot(
                x.ApplicationRequestId!.Value,
                x.TenantId,
                x.Status,
                x.UpdatedAt,
                x.CreatedAt))
            .ToListAsync(cancellationToken);
        var paymentsByApplication = pendingPayments
            .GroupBy(x => x.ApplicationId)
            .ToDictionary(x => x.Key, x => x.OrderByDescending(y => y.UpdatedAt ?? y.CreatedAt).First());

        var pendingTenants = await _context.Tenants.IgnoreQueryFilters().AsNoTracking()
            .Where(x => pendingWorkspaceIds.Contains(x.Id))
            .Select(x => new PendingTenantSnapshot(x.Id, x.Subdomain, x.WorkspaceType, x.Status, x.UpdatedAt, x.CreatedAt))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        var pendingTenantIds = pendingPayments.Select(x => x.TenantId).Concat(pendingWorkspaceIds).Distinct().ToArray();
        var pendingSubscriptions = await _context.TenantSubscriptions.AsNoTracking()
            .Where(x => pendingTenantIds.Contains(x.TenantId) && !x.IsDeleted)
            .OrderByDescending(x => x.EndDate)
            .ThenByDescending(x => x.CreatedAt)
            .Select(x => new PendingSubscriptionSnapshot(x.TenantId, x.Status, x.UpdatedAt, x.CreatedAt))
            .ToListAsync(cancellationToken);
        var subscriptionsByTenant = pendingSubscriptions
            .GroupBy(x => x.TenantId)
            .ToDictionary(x => x.Key, x => x.First());
        var pendingProvisioning = await _context.ProvisioningJobs.AsNoTracking()
            .Where(x => pendingApplicationIds.Contains(x.ApplicationRequestId))
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new PendingProvisioningSnapshot(x.ApplicationRequestId, x.Status, x.UpdatedAt, x.CreatedAt))
            .ToListAsync(cancellationToken);
        var provisioningByApplication = pendingProvisioning
            .GroupBy(x => x.ApplicationId)
            .ToDictionary(x => x.Key, x => x.First());
        var pendingMappings = await _context.TenantDatabaseMappings.AsNoTracking()
            .Where(x => pendingTenantIds.Contains(x.TenantId) && x.IsActive)
            .Select(x => new PendingMappingSnapshot(x.TenantId, x.DatabaseResourceId))
            .ToListAsync(cancellationToken);
        var mappingsByTenant = pendingMappings
            .GroupBy(x => x.TenantId)
            .ToDictionary(x => x.Key, x => x.First());
        var pendingResourceIds = pendingMappings.Select(x => x.DatabaseResourceId).Distinct().ToArray();
        var pendingResources = await _context.DatabaseResources.AsNoTracking()
            .Where(x => pendingResourceIds.Contains(x.Id))
            .Select(x => new PendingResourceSnapshot(x.Id, x.Status))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        var rawSessionToken = IdentityWorkspaceSessionToken.CreateRaw();
        _context.IdentityWorkspaceSessions.Add(new IdentityWorkspaceSession
        {
            IdentityAccountId = identity.Id,
            TokenHash = IdentityWorkspaceSessionToken.Hash(rawSessionToken),
            ExpiresAt = now.AddMinutes(10),
            CreatedByIp = _currentUserService.IpAddress
        });
        identity.LastLoginAt = now;
        await _context.SaveChangesAsync(cancellationToken);
        return new IdentitySignInDto
        {
            WorkspaceSelectionToken = rawSessionToken,
            ExpiresAt = now.AddMinutes(10),
            ActiveWorkspaces = memberships.Select(x => new IdentityWorkspaceDto { WorkspaceId = x.TenantId, Name = x.Tenant.Name, Identifier = x.Tenant.Subdomain, WorkspaceType = x.Tenant.WorkspaceType, WorkspaceStatus = x.Tenant.Status, Role = x.Role }).ToList(),
            PendingApplications = pendingApplications.Select(application =>
            {
                paymentsByApplication.TryGetValue(application.Id, out var payment);
                pendingTenants.TryGetValue(application.ProvisionedWorkspaceId ?? Guid.Empty, out var tenant);
                var tenantId = tenant?.Id ?? payment?.TenantId;
                var subscription = tenantId.HasValue && subscriptionsByTenant.TryGetValue(tenantId.Value, out var foundSubscription)
                    ? foundSubscription
                    : null;
                provisioningByApplication.TryGetValue(application.Id, out var provisioning);
                var mapping = tenantId.HasValue && mappingsByTenant.TryGetValue(tenantId.Value, out var foundMapping)
                    ? foundMapping
                    : null;
                var resource = mapping is not null && pendingResources.TryGetValue(mapping.DatabaseResourceId, out var foundResource)
                    ? foundResource
                    : null;
                return ToPendingApplicationDto(application, tenant, payment, subscription, provisioning, resource, mapping is not null);
            }).ToList(),
            RequiresWorkspaceSelection = memberships.Count != 1
        };
    }

    private static PendingApplicationDto ToPendingApplicationDto(
        ApplicationRequest application,
        PendingTenantSnapshot? tenant,
        PendingPaymentSnapshot? payment,
        PendingSubscriptionSnapshot? subscription,
        PendingProvisioningSnapshot? provisioning,
        PendingResourceSnapshot? resource,
        bool hasMapping)
    {
        var databaseStatusCode = ResolveDatabaseStatusCode(provisioning?.Status, resource?.Status, hasMapping);
        var (action, next, message) = application.Status switch
        {
            ApplicationRequestStatus.Draft => ("استكمال الطلب", "أكمل البيانات ثم أعد الإرسال", "طلبك غير مكتمل بعد."),
            ApplicationRequestStatus.NeedsMoreInformation => ("استكمال البيانات", "حدّث الحقول المطلوبة ثم أعد الإرسال", "مطلوب استكمال بعض البيانات قبل متابعة الطلب."),
            ApplicationRequestStatus.Submitted or ApplicationRequestStatus.UnderReview when provisioning?.Status is ProvisioningJobStatus.Provisioning or ProvisioningJobStatus.AwaitingDatabaseCapacity
                => ("انتظار التجهيز", "انتظر جاهزية المساحة وقاعدة البيانات", "جاري تجهيز مساحة العمل وقاعدة البيانات."),
            ApplicationRequestStatus.Submitted or ApplicationRequestStatus.UnderReview when provisioning?.Status == ProvisioningJobStatus.Failed
                => ("إعادة التجهيز", "يمكن لفريق المنصة إعادة المحاولة", "فشل تجهيز مساحة العمل ويجري التعامل معه من فريق المنصة."),
            ApplicationRequestStatus.Submitted or ApplicationRequestStatus.UnderReview when payment?.Status is PaymentRequestStatus.Pending or PaymentRequestStatus.Draft
                => ("انتظار اعتماد الدفع", "انتظر مراجعة الدفع والطلب", "تم استلام الطلب وينتظر مراجعة الدفع والبيانات."),
            ApplicationRequestStatus.Submitted or ApplicationRequestStatus.UnderReview
                => ("انتظار المراجعة", "انتظر قرار إدارة المنصة", "الطلب قيد المراجعة من إدارة المنصة."),
            _ => ("متابعة الحالة", "راجع حالة الطلب من شاشة التفعيل", "تتم متابعة طلب مساحة العمل.")
        };
        var dates = new[]
        {
            application.SubmittedAt,
            application.ReviewedAt,
            tenant?.UpdatedAt,
            tenant?.CreatedAt,
            payment?.UpdatedAt,
            payment?.CreatedAt,
            subscription?.UpdatedAt,
            subscription?.CreatedAt,
            provisioning?.UpdatedAt,
            provisioning?.CreatedAt
        };
        var workspaceType = tenant?.WorkspaceType ?? (application.ApplicationType == ApplicationType.FreelanceWorkspaceCreation
            ? WorkspaceType.FreelanceCoach
            : WorkspaceType.Gym);
        return new PendingApplicationDto
        {
            ApplicationId = application.Id,
            ApplicationType = application.ApplicationType,
            Status = application.Status,
            SubmittedAt = application.SubmittedAt,
            WorkspaceIdentifier = application.ReservedWorkspaceIdentifier ?? tenant?.Subdomain,
            WorkspaceType = workspaceType,
            PaymentStatus = payment?.Status,
            WorkspaceStatus = tenant?.Status,
            SubscriptionStatus = subscription?.Status,
            DatabaseStatusCode = databaseStatusCode,
            ProvisioningStatus = provisioning?.Status,
            CanAccessDashboard = false,
            RequiredAction = action,
            NextStep = next,
            UserMessage = message,
            LastUpdatedAtUtc = dates.Select(x => x ?? application.CreatedAt).Max()
        };
    }

    private static string ResolveDatabaseStatusCode(
        ProvisioningJobStatus? provisioningStatus,
        DatabaseResourceStatus? resourceStatus,
        bool hasMapping)
    {
        if (provisioningStatus is ProvisioningJobStatus.Provisioning or ProvisioningJobStatus.AwaitingDatabaseCapacity)
            return "Provisioning";
        if (provisioningStatus == ProvisioningJobStatus.Failed || resourceStatus == DatabaseResourceStatus.Faulted)
            return "Failed";
        if (!hasMapping)
            return "Unassigned";
        if (resourceStatus is DatabaseResourceStatus.Maintenance or DatabaseResourceStatus.RestorePending)
            return "Unavailable";
        if (resourceStatus == DatabaseResourceStatus.Retired)
            return "Released";
        return "Ready";
    }

    private sealed record PendingPaymentSnapshot(Guid ApplicationId, Guid TenantId, PaymentRequestStatus Status, DateTime? UpdatedAt, DateTime CreatedAt);
    private sealed record PendingTenantSnapshot(Guid Id, string? Subdomain, WorkspaceType WorkspaceType, TenantStatus Status, DateTime? UpdatedAt, DateTime CreatedAt);
    private sealed record PendingSubscriptionSnapshot(Guid TenantId, TenantSubscriptionStatus Status, DateTime? UpdatedAt, DateTime CreatedAt);
    private sealed record PendingProvisioningSnapshot(Guid ApplicationId, ProvisioningJobStatus Status, DateTime? UpdatedAt, DateTime CreatedAt);
    private sealed record PendingMappingSnapshot(Guid TenantId, Guid DatabaseResourceId);
    private sealed record PendingResourceSnapshot(Guid Id, DatabaseResourceStatus Status);
}
