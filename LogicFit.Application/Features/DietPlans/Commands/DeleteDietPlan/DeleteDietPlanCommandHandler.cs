using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.DietPlans.Commands.DeleteDietPlan;

public class DeleteDietPlanCommandHandler : IRequestHandler<DeleteDietPlanCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICoachPlanAccessService _accessService;

    public DeleteDietPlanCommandHandler(IApplicationDbContext context, ITenantService tenantService, ICoachPlanAccessService accessService)
    {
        _context = context;
        _tenantService = tenantService;
        _accessService = accessService;
    }

    public async Task<bool> Handle(DeleteDietPlanCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var plan = await _context.DietPlans
            .Include(p => p.Meals)
                .ThenInclude(m => m.Items)
            .FirstOrDefaultAsync(p => p.Id == request.Id && p.TenantId == tenantId, cancellationToken);

        if (plan == null)
            throw new NotFoundException("DietPlan", request.Id);

        await _accessService.EnsureCanManageDietPlanAsync(request.Id, cancellationToken);

        plan.IsDeleted = true;
        plan.DeletedAt = DateTime.UtcNow;
        foreach (var meal in plan.Meals)
        {
            meal.IsDeleted = true;
            meal.DeletedAt = DateTime.UtcNow;
            foreach (var item in meal.Items)
            {
                item.IsDeleted = true;
                item.DeletedAt = DateTime.UtcNow;
            }
        }
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
