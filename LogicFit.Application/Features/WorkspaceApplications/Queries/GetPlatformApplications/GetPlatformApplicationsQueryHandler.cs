using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Common.Models;
using LogicFit.Application.Features.WorkspaceApplications.DTOs;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.WorkspaceApplications.Queries.GetPlatformApplications;

public sealed class GetPlatformApplicationsQueryHandler
    : IRequestHandler<GetPlatformApplicationsQuery, PagedResult<PlatformApplicationDto>>
{
    private readonly IApplicationDbContext _context;

    public GetPlatformApplicationsQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<PagedResult<PlatformApplicationDto>> Handle(
        GetPlatformApplicationsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.ApplicationRequests
            .Include(x => x.IdentityAccount)
            .AsNoTracking()
            .AsQueryable();

        if (request.ApplicationType.HasValue)
            query = query.Where(x => x.ApplicationType == request.ApplicationType.Value);
        if (request.Status.HasValue)
            query = query.Where(x => x.Status == request.Status.Value);
        if (request.PaymentStatus.HasValue)
            query = query.Where(x => _context.PaymentRequests.Any(p => p.ApplicationRequestId == x.Id && p.Status == request.PaymentStatus.Value));
        if (request.WorkspaceStatus.HasValue)
            query = query.Where(x => x.ProvisionedWorkspaceId.HasValue &&
                _context.Tenants.Any(t => t.Id == x.ProvisionedWorkspaceId.Value && t.Status == request.WorkspaceStatus.Value));
        if (request.SubscriptionStatus.HasValue)
            query = query.Where(x => _context.PaymentRequests.Any(p =>
                p.ApplicationRequestId == x.Id && p.TenantSubscriptionId.HasValue &&
                _context.TenantSubscriptions.Any(s => s.Id == p.TenantSubscriptionId.Value && s.Status == request.SubscriptionStatus.Value)));
        if (request.ProvisioningStatus.HasValue)
            query = query.Where(x => _context.ProvisioningJobs.Any(j => j.ApplicationRequestId == x.Id && j.Status == request.ProvisioningStatus.Value));

        var (page, pageSize) = PageRequest.Normalize(request.Page, request.PageSize);
        var totalCount = await query.CountAsync(cancellationToken);
        var applications = await query
            .OrderByDescending(x => x.SubmittedAt)
            .ThenByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var applicationIds = applications.Select(x => x.Id).ToArray();
        var workspaceIds = applications
            .Where(x => x.ProvisionedWorkspaceId.HasValue)
            .Select(x => x.ProvisionedWorkspaceId!.Value)
            .Distinct()
            .ToArray();
        var identityIds = applications.Select(x => x.IdentityAccountId).Distinct().ToArray();

        var payments = await _context.PaymentRequests.AsNoTracking()
            .Where(x => x.ApplicationRequestId.HasValue && applicationIds.Contains(x.ApplicationRequestId.Value))
            .Select(x => new PaymentSnapshot(
                x.ApplicationRequestId!.Value,
                x.TenantId,
                x.TenantSubscriptionId,
                x.Status,
                x.UpdatedAt,
                x.CreatedAt))
            .ToListAsync(cancellationToken);
        var paymentsByApplication = payments.ToDictionary(x => x.ApplicationId);

        var tenants = await _context.Tenants.IgnoreQueryFilters().AsNoTracking()
            .Where(x => workspaceIds.Contains(x.Id))
            .Select(x => new TenantSnapshot(x.Id, x.WorkspaceType, x.Status, x.UpdatedAt, x.CreatedAt))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var tenantIds = payments.Select(x => x.TenantId).Concat(workspaceIds).Distinct().ToArray();
        var subscriptions = await _context.TenantSubscriptions.AsNoTracking()
            .Where(x => tenantIds.Contains(x.TenantId) && !x.IsDeleted)
            .OrderByDescending(x => x.EndDate)
            .ThenByDescending(x => x.CreatedAt)
            .Select(x => new SubscriptionSnapshot(x.TenantId, x.Status, x.UpdatedAt, x.CreatedAt))
            .ToListAsync(cancellationToken);
        var subscriptionsByTenant = subscriptions
            .GroupBy(x => x.TenantId)
            .ToDictionary(x => x.Key, x => x.First());

        var provisioning = await _context.ProvisioningJobs.AsNoTracking()
            .Where(x => applicationIds.Contains(x.ApplicationRequestId))
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new ProvisioningSnapshot(x.ApplicationRequestId, x.Status, x.LastErrorCode, x.UpdatedAt, x.CreatedAt))
            .ToListAsync(cancellationToken);
        var provisioningByApplication = provisioning
            .GroupBy(x => x.ApplicationId)
            .ToDictionary(x => x.Key, x => x.First());

        var mappings = await _context.TenantDatabaseMappings.AsNoTracking()
            .Where(x => workspaceIds.Contains(x.TenantId) && x.IsActive)
            .Select(x => new MappingSnapshot(x.TenantId, x.DatabaseResourceId, x.UpdatedAt, x.CreatedAt))
            .ToListAsync(cancellationToken);
        var mappingsByTenant = mappings
            .GroupBy(x => x.TenantId)
            .ToDictionary(x => x.Key, x => x.First());
        var resourceIds = mappings.Select(x => x.DatabaseResourceId).Distinct().ToArray();
        var resources = await _context.DatabaseResources.AsNoTracking()
            .Where(x => resourceIds.Contains(x.Id))
            .Select(x => new ResourceSnapshot(x.Id, x.Status))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var activeMemberships = await _context.WorkspaceMemberships.IgnoreQueryFilters().AsNoTracking()
            .Where(x => workspaceIds.Contains(x.TenantId) && identityIds.Contains(x.IdentityAccountId) &&
                        x.Status == WorkspaceMembershipStatus.Active && !x.IsDeleted)
            .Select(x => new MembershipSnapshot(x.TenantId, x.IdentityAccountId))
            .ToListAsync(cancellationToken);

        var items = applications.Select(application =>
        {
            paymentsByApplication.TryGetValue(application.Id, out var payment);
            tenants.TryGetValue(application.ProvisionedWorkspaceId ?? Guid.Empty, out var tenant);
            var tenantId = tenant?.Id ?? payment?.TenantId;
            var subscription = tenantId.HasValue && subscriptionsByTenant.TryGetValue(tenantId.Value, out var foundSubscription)
                ? foundSubscription
                : null;
            provisioningByApplication.TryGetValue(application.Id, out var job);
            var mapping = tenantId.HasValue && mappingsByTenant.TryGetValue(tenantId.Value, out var foundMapping)
                ? foundMapping
                : null;
            var resource = mapping is not null && resources.TryGetValue(mapping.DatabaseResourceId, out var foundResource)
                ? foundResource
                : null;
            var membershipReady = tenant is not null && activeMemberships.Any(x =>
                x.TenantId == tenant.Id && x.IdentityAccountId == application.IdentityAccountId);
            var databaseStatus = ResolveDatabaseStatus(job?.Status, resource?.Status, mapping is not null);
            var databaseStatusCode = ResolveDatabaseStatusCode(job?.Status, resource?.Status, mapping is not null);
            var lifecycle = ResolveLifecycle(application, tenant, payment, subscription, databaseStatus, databaseStatusCode, job, mapping, membershipReady);
            return PlatformApplicationMapper.ToDto(
                application,
                application.IdentityAccount.Email,
                application.IdentityAccount.PhoneNumber,
                lifecycle);
        }).ToList();

        return PagedResult<PlatformApplicationDto>.Create(items, totalCount, page, pageSize);
    }

    private static DatabaseResourceStatus? ResolveDatabaseStatus(
        ProvisioningJobStatus? provisioningStatus,
        DatabaseResourceStatus? resourceStatus,
        bool hasMapping)
    {
        if (resourceStatus.HasValue && hasMapping)
            return resourceStatus;
        if (provisioningStatus is ProvisioningJobStatus.Provisioning or ProvisioningJobStatus.AwaitingDatabaseCapacity)
            return DatabaseResourceStatus.Provisioning;
        if (provisioningStatus == ProvisioningJobStatus.Failed)
            return DatabaseResourceStatus.Faulted;
        if (provisioningStatus == ProvisioningJobStatus.Completed)
            return DatabaseResourceStatus.Assigned;
        return hasMapping ? DatabaseResourceStatus.Assigned : DatabaseResourceStatus.Available;
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

    private static PlatformApplicationLifecycleDto ResolveLifecycle(
        ApplicationRequest application,
        TenantSnapshot? tenant,
        PaymentSnapshot? payment,
        SubscriptionSnapshot? subscription,
        DatabaseResourceStatus? databaseStatus,
        string databaseStatusCode,
        ProvisioningSnapshot? provisioning,
        MappingSnapshot? mapping,
        bool membershipReady)
    {
        var workspaceStatus = tenant?.Status;
        var workspaceType = tenant?.WorkspaceType;
        var subscriptionStatus = subscription?.Status;
        var canAccess = application.Status == ApplicationRequestStatus.Approved
            && workspaceStatus == TenantStatus.Active
            && subscriptionStatus is TenantSubscriptionStatus.Trial or TenantSubscriptionStatus.Active or TenantSubscriptionStatus.PastDue or TenantSubscriptionStatus.GracePeriod
            && databaseStatus == DatabaseResourceStatus.Assigned
            && membershipReady;

        var (action, next, message) = application.Status switch
        {
            ApplicationRequestStatus.Rejected => ("لا يوجد إجراء", "التواصل مع الدعم عند الحاجة", "تم رفض الطلب. راجع سبب الرفض أو تواصل مع الدعم."),
            ApplicationRequestStatus.NeedsMoreInformation => ("استكمال البيانات", "تعديل الحقول المطلوبة ثم إعادة الإرسال", "مطلوب استكمال بعض البيانات قبل متابعة الطلب."),
            _ when provisioning?.Status == ProvisioningJobStatus.Failed => ("إعادة محاولة التجهيز", "إصلاح سبب الفشل ثم إعادة المحاولة", "فشل تجهيز مساحة العمل ويمكن إعادة المحاولة بأمان."),
            _ when provisioning?.Status is ProvisioningJobStatus.Provisioning or ProvisioningJobStatus.AwaitingDatabaseCapacity => ("انتظار التجهيز", "انتظار جاهزية قاعدة البيانات", "جاري تجهيز مساحة العمل وقاعدة البيانات."),
            _ when payment?.Status is PaymentRequestStatus.Pending or PaymentRequestStatus.Draft => ("مراجعة الدفع", "اعتماد الدفع قبل بدء التجهيز", "تم تسجيل الطلب وننتظر اعتماد الدفع."),
            ApplicationRequestStatus.Submitted or ApplicationRequestStatus.UnderReview => ("مراجعة الطلب", "فحص الدفع والبيانات ثم اتخاذ القرار", "الطلب قيد المراجعة من إدارة المنصة."),
            _ when subscriptionStatus is TenantSubscriptionStatus.PendingActivation or TenantSubscriptionStatus.PendingPayment => ("استكمال الاشتراك", "اعتماد الدفع وتفعيل الاشتراك", "تمت الموافقة على الطلب لكن الاشتراك لم يُفعّل بعد."),
            _ when workspaceStatus == TenantStatus.Suspended || subscriptionStatus is TenantSubscriptionStatus.Suspended or TenantSubscriptionStatus.Expired or TenantSubscriptionStatus.Cancelled => ("مراجعة الاشتراك", "تسوية سبب الإيقاف أو الانتهاء", "الوصول متوقف أو للقراءة فقط بسبب حالة الاشتراك."),
            _ when canAccess => ("لا يوجد", "يمكن فتح لوحة الإدارة", "تم تفعيل مساحة العمل ويمكن الدخول بأمان."),
            _ => ("التحقق من الجاهزية", "مراجعة حالة مساحة العمل وقاعدة البيانات", "لم تكتمل جاهزية مساحة العمل بعد.")
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
            provisioning?.CreatedAt,
            mapping?.UpdatedAt,
            mapping?.CreatedAt
        };

        return new PlatformApplicationLifecycleDto
        {
            WorkspaceType = workspaceType,
            PaymentStatus = payment?.Status,
            WorkspaceStatus = workspaceStatus,
            SubscriptionStatus = subscriptionStatus,
            DatabaseStatus = databaseStatus,
            DatabaseStatusCode = databaseStatusCode,
            ProvisioningStatus = provisioning?.Status,
            CanAccessDashboard = canAccess,
            RequiredAction = action,
            NextStep = next,
            UserMessage = message,
            ProvisioningErrorCode = provisioning?.LastErrorCode,
            LastUpdatedAtUtc = dates.Where(x => x.HasValue).Select(x => x!.Value).DefaultIfEmpty(application.CreatedAt).Max()
        };
    }

    private sealed record PaymentSnapshot(
        Guid ApplicationId,
        Guid TenantId,
        Guid? TenantSubscriptionId,
        PaymentRequestStatus Status,
        DateTime? UpdatedAt,
        DateTime CreatedAt);

    private sealed record TenantSnapshot(
        Guid Id,
        WorkspaceType WorkspaceType,
        TenantStatus Status,
        DateTime? UpdatedAt,
        DateTime CreatedAt);

    private sealed record SubscriptionSnapshot(
        Guid TenantId,
        TenantSubscriptionStatus Status,
        DateTime? UpdatedAt,
        DateTime CreatedAt);

    private sealed record ProvisioningSnapshot(
        Guid ApplicationId,
        ProvisioningJobStatus Status,
        string? LastErrorCode,
        DateTime? UpdatedAt,
        DateTime CreatedAt);

    private sealed record MappingSnapshot(
        Guid TenantId,
        Guid DatabaseResourceId,
        DateTime? UpdatedAt,
        DateTime CreatedAt);

    private sealed record ResourceSnapshot(Guid Id, DatabaseResourceStatus Status);

    private sealed record MembershipSnapshot(Guid TenantId, Guid IdentityAccountId);
}
