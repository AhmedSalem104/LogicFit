using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.TenantBilling.DTOs;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.TenantBilling.Commands.ChooseSubscriptionPlan;

public class ChooseSubscriptionPlanCommandHandler : IRequestHandler<ChooseSubscriptionPlanCommand, TenantSubscriptionSummaryDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public ChooseSubscriptionPlanCommandHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<TenantSubscriptionSummaryDto> Handle(ChooseSubscriptionPlanCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var plan = await _context.Plans
            .FirstOrDefaultAsync(p => p.Id == request.PlanId && p.IsActive, cancellationToken)
            ?? throw new NotFoundException(nameof(Plan), request.PlanId);

        // Reuse an existing pending subscription for this plan, or open a new one.
        var subscription = await _context.TenantSubscriptions
            .Where(s => s.TenantId == tenantId && s.PlanId == plan.Id &&
                        s.Status == TenantSubscriptionStatus.PendingPayment)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (subscription == null)
        {
            subscription = new TenantSubscription
            {
                TenantId = tenantId,
                PlanId = plan.Id,
                Status = TenantSubscriptionStatus.PendingPayment,
                BillingCycle = plan.BillingCycle,
                Amount = plan.Price,
                Currency = plan.Currency
            };
            _context.TenantSubscriptions.Add(subscription);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return new TenantSubscriptionSummaryDto
        {
            SubscriptionId = subscription.Id,
            PlanId = plan.Id,
            PlanName = plan.Name,
            Status = subscription.Status,
            Amount = subscription.Amount,
            Currency = subscription.Currency
        };
    }
}
