using LogicFit.Application.Features.TenantBilling.DTOs;
using MediatR;

namespace LogicFit.Application.Features.TenantBilling.Commands.ChooseSubscriptionPlan;

/// <summary>Owner selects/upgrades to a plan — opens (or reuses) a PendingPayment subscription to pay for.</summary>
public class ChooseSubscriptionPlanCommand : IRequest<TenantSubscriptionSummaryDto>
{
    public Guid PlanId { get; set; }
}
