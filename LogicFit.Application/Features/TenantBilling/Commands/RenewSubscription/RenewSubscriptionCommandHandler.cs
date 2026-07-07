using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.TenantBilling.Commands.ChooseSubscriptionPlan;
using LogicFit.Application.Features.TenantBilling.DTOs;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.TenantBilling.Commands.RenewSubscription;

public class RenewSubscriptionCommandHandler : IRequestHandler<RenewSubscriptionCommand, TenantSubscriptionSummaryDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly IMediator _mediator;

    public RenewSubscriptionCommandHandler(IApplicationDbContext context, ITenantService tenantService, IMediator mediator)
    {
        _context = context;
        _tenantService = tenantService;
        _mediator = mediator;
    }

    public async Task<TenantSubscriptionSummaryDto> Handle(RenewSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        // The plan of the most recent subscription (active or otherwise).
        var currentPlanId = await _context.TenantSubscriptions
            .Where(s => s.TenantId == tenantId)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => (Guid?)s.PlanId)
            .FirstOrDefaultAsync(cancellationToken);

        if (currentPlanId == null)
        {
            throw new NotFoundException("No subscription to renew. Select a plan first.");
        }

        return await _mediator.Send(new ChooseSubscriptionPlanCommand { PlanId = currentPlanId.Value }, cancellationToken);
    }
}
