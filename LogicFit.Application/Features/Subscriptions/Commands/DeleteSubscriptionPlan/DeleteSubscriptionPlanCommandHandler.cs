using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Subscriptions.Commands.DeleteSubscriptionPlan;

public class DeleteSubscriptionPlanCommandHandler : IRequestHandler<DeleteSubscriptionPlanCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public DeleteSubscriptionPlanCommandHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<bool> Handle(DeleteSubscriptionPlanCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var plan = await _context.SubscriptionPlans
            .FirstOrDefaultAsync(p => p.Id == request.Id && p.TenantId == tenantId, cancellationToken);

        if (plan == null)
            throw new NotFoundException("SubscriptionPlan", request.Id);

        var hasActiveSubscriptions = await _context.ClientSubscriptions
            .AnyAsync(s => s.PlanId == request.Id && s.TenantId == tenantId
                && (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Suspended),
                cancellationToken);

        if (hasActiveSubscriptions)
            throw new ConflictException("Cannot delete plan with active subscriptions. Deactivate the plan instead.");

        _context.SubscriptionPlans.Remove(plan);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
