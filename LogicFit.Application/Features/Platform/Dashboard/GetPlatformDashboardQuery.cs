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
}
