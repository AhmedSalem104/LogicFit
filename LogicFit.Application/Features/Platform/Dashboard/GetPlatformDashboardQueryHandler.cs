using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Authorization;
using LogicFit.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Platform.Dashboard;

public class GetPlatformDashboardQueryHandler : IRequestHandler<GetPlatformDashboardQuery, PlatformDashboardDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IDatabaseRestoreService _restoreService;

    public GetPlatformDashboardQueryHandler(IApplicationDbContext context, IDatabaseRestoreService restoreService)
    {
        _context = context;
        _restoreService = restoreService;
    }

    public async Task<PlatformDashboardDto> Handle(GetPlatformDashboardQuery request, CancellationToken cancellationToken)
    {
        var tenants = _context.Tenants.Where(t => t.Id != PlatformConstants.PlatformTenantId);
        var subscriptions = _context.TenantSubscriptions.AsNoTracking().IgnoreQueryFilters();
        if (request.TenantId.HasValue) subscriptions = subscriptions.Where(x => x.TenantId == request.TenantId.Value);
        if (request.PlanId.HasValue) subscriptions = subscriptions.Where(x => x.PlanId == request.PlanId.Value);
        if (request.SubscriptionStatus.HasValue) subscriptions = subscriptions.Where(x => x.Status == request.SubscriptionStatus.Value);
        if (request.FromUtc.HasValue) subscriptions = subscriptions.Where(x => x.CreatedAt >= request.FromUtc.Value);
        if (request.ToUtc.HasValue) subscriptions = subscriptions.Where(x => x.CreatedAt < request.ToUtc.Value);
        var invoices = _context.SubscriptionInvoices.AsNoTracking().IgnoreQueryFilters();
        var payments = _context.SubscriptionPayments.AsNoTracking();

        var restoreCapabilities = _restoreService.GetCapabilities();
        var applications = _context.ApplicationRequests.AsNoTracking();
        var paymentRequests = _context.PaymentRequests.AsNoTracking().IgnoreQueryFilters().Where(x => !x.IsDeleted);
        var resources = _context.DatabaseResources.AsNoTracking();
        var provisioning = _context.ProvisioningJobs.AsNoTracking();
        var batches = _context.BackupBatches.AsNoTracking();
        var artifacts = _context.DatabaseBackups.AsNoTracking();
        var restores = _context.RestoreJobs.AsNoTracking();

        return new PlatformDashboardDto
        {
            TotalGyms = await tenants.CountAsync(cancellationToken),
            ActiveGyms = await tenants.CountAsync(t => t.Status == TenantStatus.Active, cancellationToken),
            TrialGyms = await tenants.CountAsync(t => t.Status == TenantStatus.Trial, cancellationToken),
            PendingApprovalGyms = await tenants.CountAsync(t => t.Status == TenantStatus.PendingApproval, cancellationToken),
            SuspendedGyms = await tenants.CountAsync(t => t.Status == TenantStatus.Suspended, cancellationToken),
            TotalMembers = await _context.Users.IgnoreQueryFilters().CountAsync(u => u.Role == UserRole.Client && !u.IsDeleted, cancellationToken),
            ExpiredSubscriptions = await subscriptions.CountAsync(x => x.Status == TenantSubscriptionStatus.Expired && !x.IsDeleted, cancellationToken),
            ActiveSubscriptions = await subscriptions.CountAsync(x => x.Status == TenantSubscriptionStatus.Active && !x.IsDeleted, cancellationToken),
            PendingPayments = await _context.PaymentRequests.IgnoreQueryFilters().CountAsync(x => x.Status == PaymentRequestStatus.Pending && !x.IsDeleted, cancellationToken),
            InvoiceCount = await invoices.CountAsync(x => !x.IsDeleted, cancellationToken),
            InvoicedAmount = await invoices.Where(x => !x.IsDeleted).SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m,
            CollectedAmount = await payments.SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m,
            FeatureCount = await _context.Features.CountAsync(cancellationToken),
            QuotaDefinitionCount = await _context.FeatureQuotaDefinitions.CountAsync(x => x.IsActive, cancellationToken),
            FailedJobs = await _context.JobExecutionLogs.CountAsync(x => x.Status == "Failed", cancellationToken),
            FailedOutbox = await _context.OutboxMessages.CountAsync(x => x.FailedAtUtc != null && x.ProcessedAtUtc == null, cancellationToken),
            Operations = new PlatformOperationsSummaryDto
            {
                Applications = new ApplicationReviewSummaryDto
                {
                    Draft = await applications.CountAsync(x => x.Status == ApplicationRequestStatus.Draft, cancellationToken),
                    Submitted = await applications.CountAsync(x => x.Status == ApplicationRequestStatus.Submitted, cancellationToken),
                    UnderReview = await applications.CountAsync(x => x.Status == ApplicationRequestStatus.UnderReview, cancellationToken),
                    NeedsMoreInformation = await applications.CountAsync(x => x.Status == ApplicationRequestStatus.NeedsMoreInformation, cancellationToken),
                    Approved = await applications.CountAsync(x => x.Status == ApplicationRequestStatus.Approved, cancellationToken),
                    Rejected = await applications.CountAsync(x => x.Status == ApplicationRequestStatus.Rejected, cancellationToken),
                    GymWorkspaceCreation = await applications.CountAsync(x => x.ApplicationType == ApplicationType.GymWorkspaceCreation, cancellationToken),
                    FreelanceWorkspaceCreation = await applications.CountAsync(x => x.ApplicationType == ApplicationType.FreelanceWorkspaceCreation, cancellationToken),
                    Membership = await applications.CountAsync(x => x.ApplicationType == ApplicationType.CoachMembership ||
                        x.ApplicationType == ApplicationType.AssistantMembership ||
                        x.ApplicationType == ApplicationType.ClientMembership, cancellationToken)
                },
                Payments = new PaymentReviewSummaryDto
                {
                    PendingReview = await paymentRequests.CountAsync(x => x.Status == PaymentRequestStatus.Pending, cancellationToken),
                    Approved = await paymentRequests.CountAsync(x => x.Status == PaymentRequestStatus.Approved, cancellationToken),
                    Rejected = await paymentRequests.CountAsync(x => x.Status == PaymentRequestStatus.Rejected, cancellationToken),
                    PendingAmount = await paymentRequests.Where(x => x.Status == PaymentRequestStatus.Pending).SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m
                },
                DatabasePool = new DatabasePoolSummaryDto
                {
                    Total = await resources.CountAsync(cancellationToken),
                    Available = await resources.CountAsync(x => x.Status == DatabaseResourceStatus.Available, cancellationToken),
                    Reserved = await resources.CountAsync(x => x.Status == DatabaseResourceStatus.Reserved, cancellationToken),
                    Provisioning = await resources.CountAsync(x => x.Status == DatabaseResourceStatus.Provisioning, cancellationToken),
                    Assigned = await resources.CountAsync(x => x.Status == DatabaseResourceStatus.Assigned, cancellationToken),
                    Maintenance = await resources.CountAsync(x => x.Status == DatabaseResourceStatus.Maintenance, cancellationToken),
                    RestorePending = await resources.CountAsync(x => x.Status == DatabaseResourceStatus.RestorePending, cancellationToken),
                    Faulted = await resources.CountAsync(x => x.Status == DatabaseResourceStatus.Faulted, cancellationToken),
                    Retired = await resources.CountAsync(x => x.Status == DatabaseResourceStatus.Retired, cancellationToken),
                    ActiveMappings = await _context.TenantDatabaseMappings.CountAsync(x => x.IsActive, cancellationToken)
                },
                Provisioning = new ProvisioningSummaryDto
                {
                    Pending = await provisioning.CountAsync(x => x.Status == ProvisioningJobStatus.Pending, cancellationToken),
                    AwaitingDatabaseCapacity = await provisioning.CountAsync(x => x.Status == ProvisioningJobStatus.AwaitingDatabaseCapacity, cancellationToken),
                    Provisioning = await provisioning.CountAsync(x => x.Status == ProvisioningJobStatus.Provisioning, cancellationToken),
                    Completed = await provisioning.CountAsync(x => x.Status == ProvisioningJobStatus.Completed, cancellationToken),
                    Failed = await provisioning.CountAsync(x => x.Status == ProvisioningJobStatus.Failed, cancellationToken)
                },
                Backups = new BackupSummaryDto
                {
                    TotalBatches = await batches.CountAsync(cancellationToken),
                    RunningBatches = await batches.CountAsync(x => x.Status == BackupBatchStatus.Running, cancellationToken),
                    CompletedBatches = await batches.CountAsync(x => x.Status == BackupBatchStatus.Completed, cancellationToken),
                    FailedBatches = await batches.CountAsync(x => x.Status == BackupBatchStatus.Failed, cancellationToken),
                    FailedArtifacts = await artifacts.CountAsync(x => x.Status == DatabaseBackupStatus.Failed, cancellationToken),
                    LastCompletedAtUtc = await batches.Where(x => x.Status == BackupBatchStatus.Completed && x.CompletedAtUtc != null).MaxAsync(x => (DateTime?)x.CompletedAtUtc, cancellationToken)
                },
                Restores = new RestoreSummaryDto
                {
                    TotalJobs = await restores.CountAsync(cancellationToken),
                    PendingJobs = await restores.CountAsync(x => x.Status == RestoreJobStatus.Pending, cancellationToken),
                    RunningJobs = await restores.CountAsync(x => x.Status == RestoreJobStatus.Running, cancellationToken),
                    CompletedJobs = await restores.CountAsync(x => x.Status == RestoreJobStatus.Completed, cancellationToken),
                    FailedJobs = await restores.CountAsync(x => x.Status == RestoreJobStatus.Failed, cancellationToken),
                    Capabilities = new DatabaseRestoreCapabilitiesDto
                    {
                        Enabled = restoreCapabilities.Enabled,
                        Mode = restoreCapabilities.Mode,
                        SupportsBacpacImport = restoreCapabilities.SupportsBacpacImport,
                        SupportsMappingSwitch = restoreCapabilities.SupportsMappingSwitch,
                        UnavailableReason = restoreCapabilities.UnavailableReason
                    }
                }
            }
        };
    }
}
