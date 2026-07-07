using LogicFit.Application.Features.TenantBilling.DTOs;
using MediatR;

namespace LogicFit.Application.Features.TenantBilling.Commands.RenewSubscription;

/// <summary>Owner renews the current plan — opens a PendingPayment subscription for it to pay for.</summary>
public class RenewSubscriptionCommand : IRequest<TenantSubscriptionSummaryDto>
{
}
