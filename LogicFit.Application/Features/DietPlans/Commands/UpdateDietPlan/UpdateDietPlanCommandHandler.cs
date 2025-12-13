using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.DietPlans.Commands.UpdateDietPlan;

public class UpdateDietPlanCommandHandler : IRequestHandler<UpdateDietPlanCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public UpdateDietPlanCommandHandler(
        IApplicationDbContext context,
        ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<bool> Handle(UpdateDietPlanCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var plan = await _context.DietPlans
            .FirstOrDefaultAsync(p => p.Id == request.Id && p.TenantId == tenantId, cancellationToken);

        if (plan == null)
            throw new NotFoundException("DietPlan", request.Id);

        plan.Name = request.Name;
        plan.StartDate = request.StartDate;
        plan.EndDate = request.EndDate;
        plan.TargetCalories = request.TargetCalories;
        plan.TargetProtein = request.TargetProtein;
        plan.TargetCarbs = request.TargetCarbs;
        plan.TargetFats = request.TargetFats;
        plan.Status = request.Status;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
