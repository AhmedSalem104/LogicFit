using System.Text.Json;
using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.WorkspaceApplications.DTOs;
using LogicFit.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.WorkspaceApplications.Queries.GetApplicationTrackingStatus;

public sealed class GetApplicationTrackingStatusQueryHandler
    : IRequestHandler<GetApplicationTrackingStatusQuery, ApplicationTrackingStatusDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeService _dateTimeService;

    public GetApplicationTrackingStatusQueryHandler(IApplicationDbContext context, IDateTimeService dateTimeService)
    {
        _context = context;
        _dateTimeService = dateTimeService;
    }

    public async Task<ApplicationTrackingStatusDto> Handle(GetApplicationTrackingStatusQuery request, CancellationToken cancellationToken)
    {
        var session = await ApplicationTrackingSessionResolver.GetActiveAsync(
            _context, _dateTimeService, request.TrackingToken, cancellationToken);
        var application = session.ApplicationRequest;
        var payment = await _context.PaymentRequests
            .Include(x => x.Proofs)
            .AsNoTracking()
            .Where(x => x.ApplicationRequestId == application.Id)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        var tenant = application.ProvisionedWorkspaceId.HasValue
            ? await _context.Tenants.IgnoreQueryFilters().AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == application.ProvisionedWorkspaceId.Value, cancellationToken)
            : null;
        var tenantId = tenant?.Id ?? payment?.TenantId;
        var subscription = tenantId.HasValue
            ? await _context.TenantSubscriptions.AsNoTracking()
                .Where(x => x.TenantId == tenantId.Value && !x.IsDeleted)
                .OrderByDescending(x => x.EndDate)
                .ThenByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken)
            : null;
        var provisioning = await _context.ProvisioningJobs.AsNoTracking()
            .Where(x => x.ApplicationRequestId == application.Id)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        var mapping = tenantId.HasValue
            ? await _context.TenantDatabaseMappings.AsNoTracking()
                .Where(x => x.TenantId == tenantId.Value && x.IsActive)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken)
            : null;
        var resource = mapping is null
            ? null
            : await _context.DatabaseResources.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == mapping.DatabaseResourceId, cancellationToken);
        var membershipReady = tenant is not null && await _context.WorkspaceMemberships
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(x => x.TenantId == tenant.Id && x.IdentityAccountId == application.IdentityAccountId &&
                          x.Status == WorkspaceMembershipStatus.Active && !x.IsDeleted, cancellationToken);
        var requestedFields = ReadStringList(application.RequestedFieldsJson);
        var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(application.PayloadJson)
            ?? new Dictionary<string, JsonElement>();
        var editable = payload
            .Where(x => requestedFields.Contains(x.Key, StringComparer.Ordinal))
            .ToDictionary(x => x.Key, x => x.Value.Clone(), StringComparer.Ordinal);

        var databaseStatus = ResolveDatabaseStatus(provisioning?.Status, resource?.Status, mapping is not null);
        var databaseStatusCode = ResolveDatabaseStatusCode(provisioning?.Status, resource?.Status, mapping is not null);
        var canAccess = application.Status == ApplicationRequestStatus.Approved
            && tenant?.Status == TenantStatus.Active
            && subscription?.Status is TenantSubscriptionStatus.Trial or TenantSubscriptionStatus.Active or TenantSubscriptionStatus.PastDue or TenantSubscriptionStatus.GracePeriod
            && databaseStatus == DatabaseResourceStatus.Assigned
            && membershipReady;
        var (action, next, message) = ResolveLifecycle(application.Status, payment?.Status, tenant?.Status, subscription?.Status, provisioning?.Status, canAccess);
        var userJourneyStage = ResolveUserJourneyStage(application.Status, payment?.Status, tenant?.Status, provisioning?.Status, canAccess);
        var dates = new[]
        {
            application.SubmittedAt,
            application.ReviewedAt,
            payment?.UpdatedAt,
            payment?.CreatedAt,
            tenant?.UpdatedAt,
            tenant?.CreatedAt,
            subscription?.UpdatedAt,
            subscription?.CreatedAt,
            provisioning?.UpdatedAt,
            provisioning?.CreatedAt,
            mapping?.UpdatedAt,
            mapping?.CreatedAt
        };

        return new ApplicationTrackingStatusDto
        {
            ApplicationId = application.Id,
            ApplicationType = application.ApplicationType,
            Status = application.Status,
            WorkspaceIdentifier = application.ReservedWorkspaceIdentifier,
            InformationRequest = application.InformationRequest,
            RequestedFields = requestedFields,
            SubmittedAt = application.SubmittedAt,
            ReviewedAt = application.ReviewedAt,
            PlanId = application.PlanId,
            BillingCycle = application.BillingCycle,
            PlanSnapshotJson = application.PlanSnapshotJson,
            PaymentRequestId = payment?.Id,
            PaymentStatus = payment?.Status,
            PaymentProofVersion = payment?.Proofs.FirstOrDefault(x => x.IsCurrent)?.Version ?? 0,
            WorkspaceType = tenant?.WorkspaceType ?? (application.ApplicationType == ApplicationType.FreelanceWorkspaceCreation ? WorkspaceType.FreelanceCoach : WorkspaceType.Gym),
            WorkspaceStatus = tenant?.Status,
            SubscriptionStatus = subscription?.Status,
            DatabaseStatus = databaseStatus,
            DatabaseStatusCode = databaseStatusCode,
            ProvisioningStatus = provisioning?.Status,
            UserJourneyStage = userJourneyStage,
            CanAccessDashboard = canAccess,
            RequiredAction = action,
            NextStep = next,
            UserMessage = message,
            LastUpdatedAtUtc = dates.Where(x => x.HasValue).Select(x => x!.Value).DefaultIfEmpty(application.CreatedAt).Max(),
            ProvisioningErrorCode = provisioning?.LastErrorCode,
            EditableValues = editable
        };
    }

    private static DatabaseResourceStatus ResolveDatabaseStatus(
        ProvisioningJobStatus? provisioningStatus,
        DatabaseResourceStatus? resourceStatus,
        bool hasMapping)
    {
        if (resourceStatus.HasValue && hasMapping)
            return resourceStatus.Value;
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

    private static (string Action, string Next, string Message) ResolveLifecycle(
        ApplicationRequestStatus applicationStatus,
        PaymentRequestStatus? paymentStatus,
        TenantStatus? workspaceStatus,
        TenantSubscriptionStatus? subscriptionStatus,
        ProvisioningJobStatus? provisioningStatus,
        bool canAccess)
    {
        if (applicationStatus == ApplicationRequestStatus.Rejected)
            return ("لا يوجد إجراء", "التواصل مع الدعم عند الحاجة", "تم رفض الطلب. راجع سبب الرفض أو تواصل مع الدعم.");
        if (paymentStatus is PaymentRequestStatus.Rejected or PaymentRequestStatus.Cancelled or PaymentRequestStatus.Expired)
            return ("مراجعة الدفع", "تواصل مع الدعم أو أرسل وسيلة دفع صحيحة", "لم يتم اعتماد الدفع. راجع بيانات الدفع أو تواصل مع الدعم.");
        if (applicationStatus == ApplicationRequestStatus.NeedsMoreInformation)
            return ("استكمال البيانات", "حدّث الحقول المطلوبة ثم أعد الإرسال", "مطلوب استكمال بعض البيانات قبل متابعة الطلب.");
        if (provisioningStatus == ProvisioningJobStatus.Failed)
            return ("إعادة التجهيز", "يمكن لفريق المنصة إعادة المحاولة", "فشل تجهيز مساحة العمل ويجري التعامل معه من فريق المنصة.");
        if (provisioningStatus is ProvisioningJobStatus.Provisioning or ProvisioningJobStatus.AwaitingDatabaseCapacity)
            return ("انتظار التجهيز", "انتظر جاهزية المساحة وقاعدة البيانات", "جاري تجهيز مساحة العمل وقاعدة البيانات.");
        if (paymentStatus is PaymentRequestStatus.Pending or PaymentRequestStatus.Draft)
            return ("انتظار اعتماد الدفع", "انتظر مراجعة الدفع والطلب", "تم استلام الطلب وينتظر مراجعة الدفع والبيانات.");
        if (applicationStatus is ApplicationRequestStatus.Submitted or ApplicationRequestStatus.UnderReview)
            return ("انتظار المراجعة", "انتظر قرار إدارة المنصة", "الطلب قيد المراجعة من إدارة المنصة.");
        if (subscriptionStatus is TenantSubscriptionStatus.PendingActivation or TenantSubscriptionStatus.PendingPayment)
            return ("استكمال الاشتراك", "اعتماد الدفع وتفعيل الاشتراك", "تمت الموافقة على الطلب لكن الاشتراك لم يُفعّل بعد.");
        if (workspaceStatus == TenantStatus.Suspended || subscriptionStatus is TenantSubscriptionStatus.Suspended or TenantSubscriptionStatus.Expired or TenantSubscriptionStatus.Cancelled)
            return ("مراجعة الاشتراك", "تسوية سبب الإيقاف أو الانتهاء", "الوصول متوقف بسبب حالة الاشتراك أو مساحة العمل.");
        if (canAccess)
            return ("لا يوجد", "يمكن فتح لوحة الإدارة", "تم تفعيل مساحة العمل ويمكن الدخول بأمان.");
        return ("التحقق من الجاهزية", "مراجعة حالة مساحة العمل وقاعدة البيانات", "لم تكتمل جاهزية مساحة العمل بعد.");
    }

    private static string ResolveUserJourneyStage(
        ApplicationRequestStatus applicationStatus,
        PaymentRequestStatus? paymentStatus,
        TenantStatus? workspaceStatus,
        ProvisioningJobStatus? provisioningStatus,
        bool canAccess)
    {
        if (canAccess)
            return "Ready";
        if (applicationStatus == ApplicationRequestStatus.Rejected)
            return "Rejected";
        if (paymentStatus is PaymentRequestStatus.Rejected or PaymentRequestStatus.Cancelled or PaymentRequestStatus.Expired)
            return "PaymentRejected";
        if (applicationStatus == ApplicationRequestStatus.NeedsMoreInformation)
            return "MoreInformation";
        if (provisioningStatus is ProvisioningJobStatus.Provisioning or ProvisioningJobStatus.AwaitingDatabaseCapacity ||
            workspaceStatus is TenantStatus.Provisioning or TenantStatus.AwaitingDatabaseCapacity or TenantStatus.ProvisioningFailed)
            return "Preparing";
        if (applicationStatus is ApplicationRequestStatus.UnderReview or ApplicationRequestStatus.Approved)
            return "UnderReview";
        return "Submitted";
    }

    internal static IReadOnlyList<string> ReadStringList(string? json) => string.IsNullOrWhiteSpace(json)
        ? Array.Empty<string>()
        : JsonSerializer.Deserialize<string[]>(json) ?? Array.Empty<string>();
}
