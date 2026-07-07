using LogicFit.Domain.Enums;

namespace LogicFit.Application.Features.TenantBilling.DTOs;

public class TenantSubscriptionSummaryDto
{
    public Guid SubscriptionId { get; set; }
    public Guid PlanId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public TenantSubscriptionStatus Status { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EGP";
}
