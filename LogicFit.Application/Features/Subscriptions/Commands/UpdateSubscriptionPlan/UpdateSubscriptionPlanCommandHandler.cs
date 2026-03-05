using System.Text.Json;
using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Subscriptions.Commands.UpdateSubscriptionPlan;

public class UpdateSubscriptionPlanCommandHandler : IRequestHandler<UpdateSubscriptionPlanCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public UpdateSubscriptionPlanCommandHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<bool> Handle(UpdateSubscriptionPlanCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var plan = await _context.SubscriptionPlans
            .FirstOrDefaultAsync(p => p.Id == request.Id && p.TenantId == tenantId, cancellationToken);

        if (plan == null)
            throw new NotFoundException("SubscriptionPlan", request.Id);

        plan.Name = request.Name;
        plan.Price = request.Price;
        plan.DurationMonths = request.DurationMonths;
        plan.Description = request.Description;
        plan.Features = request.Features != null ? JsonSerializer.Serialize(request.Features) : null;
        plan.MaxFreezeDays = request.MaxFreezeDays;
        plan.MaxFreezeCount = request.MaxFreezeCount;
        plan.IsActive = request.IsActive;
        plan.SessionsPerWeek = request.SessionsPerWeek;
        plan.InBodyIncluded = request.InBodyIncluded;
        plan.PrivateCoach = request.PrivateCoach;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
