using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.DietPlans.Commands.DeleteDailyMeal;

public class DeleteDailyMealCommandHandler : IRequestHandler<DeleteDailyMealCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICoachPlanAccessService _accessService;

    public DeleteDailyMealCommandHandler(
        IApplicationDbContext context,
        ITenantService tenantService,
        ICoachPlanAccessService accessService)
    {
        _context = context;
        _tenantService = tenantService;
        _accessService = accessService;
    }

    public async Task<bool> Handle(DeleteDailyMealCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var meal = await _context.DailyMeals
            .Include(m => m.Items)
            .FirstOrDefaultAsync(m => m.Id == request.Id && m.TenantId == tenantId, cancellationToken);

        if (meal == null)
            throw new NotFoundException("DailyMeal", request.Id);

        await _accessService.EnsureCanManageMealAsync(request.Id, cancellationToken);

        // Delete all items first
        _context.MealItems.RemoveRange(meal.Items);

        // Then delete the meal
        _context.DailyMeals.Remove(meal);

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
