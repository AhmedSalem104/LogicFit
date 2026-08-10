using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.DietPlans.Commands.CreateDailyMeal;

public class CreateDailyMealCommandHandler : IRequestHandler<CreateDailyMealCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICoachPlanAccessService _accessService;

    public CreateDailyMealCommandHandler(IApplicationDbContext context, ITenantService tenantService, ICoachPlanAccessService accessService)
    {
        _context = context;
        _tenantService = tenantService;
        _accessService = accessService;
    }

    public async Task<Guid> Handle(CreateDailyMealCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var plan = await _context.DietPlans
            .FirstOrDefaultAsync(p => p.Id == request.PlanId && p.TenantId == tenantId, cancellationToken);

        if (plan == null)
            throw new NotFoundException("DietPlan", request.PlanId);

        await _accessService.EnsureCanManageDietPlanAsync(request.PlanId, cancellationToken);

        var meal = new DailyMeal
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PlanId = request.PlanId,
            Name = request.Name,
            OrderIndex = request.OrderIndex,
            Time = request.Time
        };

        _context.DailyMeals.Add(meal);
        await _context.SaveChangesAsync(cancellationToken);

        return meal.Id;
    }
}
