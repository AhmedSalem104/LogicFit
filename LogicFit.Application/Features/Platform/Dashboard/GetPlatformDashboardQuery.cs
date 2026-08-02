using MediatR;
using LogicFit.Domain.Enums;

namespace LogicFit.Application.Features.Platform.Dashboard;

public class GetPlatformDashboardQuery : IRequest<PlatformDashboardDto>
{
    public DateTime? FromUtc { get; init; }
    public DateTime? ToUtc { get; init; }
    public Guid? TenantId { get; init; }
    public Guid? PlanId { get; init; }
    public TenantSubscriptionStatus? SubscriptionStatus { get; init; }
}

public class PlatformDashboardDto
{
    public int TotalGyms { get; set; }
    public int ActiveGyms { get; set; }
    public int TrialGyms { get; set; }
    public int PendingApprovalGyms { get; set; }
    public int SuspendedGyms { get; set; }
    public int TotalMembers { get; set; }
    public int ExpiredSubscriptions { get; set; }
    public int ActiveSubscriptions { get; set; }
    public int PendingPayments { get; set; }
    public int InvoiceCount { get; set; }
    public decimal InvoicedAmount { get; set; }
    public decimal CollectedAmount { get; set; }
    public int FeatureCount { get; set; }
    public int QuotaDefinitionCount { get; set; }
    public int FailedJobs { get; set; }
    public int FailedOutbox { get; set; }
    /// <summary>
    /// Permission-filtered operational counters used by the Platform dashboard. The values are
    /// aggregate metadata only; database names, connection material and private artifacts are
    /// deliberately excluded from this contract.
    /// </summary>
    public PlatformOperationsSummaryDto Operations { get; set; } = new();
}

public sealed class PlatformOperationsSummaryDto
{
    public ApplicationReviewSummaryDto Applications { get; set; } = new();
    public PaymentReviewSummaryDto Payments { get; set; } = new();
    public DatabasePoolSummaryDto DatabasePool { get; set; } = new();
    public ProvisioningSummaryDto Provisioning { get; set; } = new();
    public BackupSummaryDto Backups { get; set; } = new();
    public RestoreSummaryDto Restores { get; set; } = new();
}

public sealed class ApplicationReviewSummaryDto
{
    public int Draft { get; set; }
    public int Submitted { get; set; }
    public int UnderReview { get; set; }
    public int NeedsMoreInformation { get; set; }
    public int Approved { get; set; }
    public int Rejected { get; set; }
    public int GymWorkspaceCreation { get; set; }
    public int FreelanceWorkspaceCreation { get; set; }
    public int Membership { get; set; }
}

public sealed class PaymentReviewSummaryDto
{
    public int PendingReview { get; set; }
    public int Approved { get; set; }
    public int Rejected { get; set; }
    public decimal PendingAmount { get; set; }
}

public sealed class DatabasePoolSummaryDto
{
    public int Total { get; set; }
    public int Available { get; set; }
    public int Reserved { get; set; }
    public int Provisioning { get; set; }
    public int Assigned { get; set; }
    public int Maintenance { get; set; }
    public int RestorePending { get; set; }
    public int Faulted { get; set; }
    public int Retired { get; set; }
    public int ActiveMappings { get; set; }
}

public sealed class ProvisioningSummaryDto
{
    public int Pending { get; set; }
    public int AwaitingDatabaseCapacity { get; set; }
    public int Provisioning { get; set; }
    public int Completed { get; set; }
    public int Failed { get; set; }
}

public sealed class BackupSummaryDto
{
    public int TotalBatches { get; set; }
    public int RunningBatches { get; set; }
    public int CompletedBatches { get; set; }
    public int FailedBatches { get; set; }
    public int FailedArtifacts { get; set; }
    public DateTime? LastCompletedAtUtc { get; set; }
}

public sealed class RestoreSummaryDto
{
    public int TotalJobs { get; set; }
    public int PendingJobs { get; set; }
    public int RunningJobs { get; set; }
    public int CompletedJobs { get; set; }
    public int FailedJobs { get; set; }
    public DatabaseRestoreCapabilitiesDto Capabilities { get; set; } = new();
}

public sealed class DatabaseRestoreCapabilitiesDto
{
    public bool Enabled { get; set; }
    public string Mode { get; set; } = string.Empty;
    public bool SupportsBacpacImport { get; set; }
    public bool SupportsMappingSwitch { get; set; }
    public string? UnavailableReason { get; set; }
}
