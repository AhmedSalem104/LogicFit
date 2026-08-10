using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.DietPlans.Commands.UpdateMealItem;

public class UpdateMealItemCommandHandler : IRequestHandler<UpdateMealItemCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICoachPlanAccessService _accessService;

    public UpdateMealItemCommandHandler(
        IApplicationDbContext context,
        ITenantService tenantService,
        ICoachPlanAccessService accessService)
    {
        _context = context;
        _tenantService = tenantService;
        _accessService = accessService;
    }

    public async Task<bool> Handle(UpdateMealItemCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var item = await _context.MealItems
            .Include(i => i.Food)
            .Include(i => i.Meal)
            .FirstOrDefaultAsync(i => i.Id == request.Id && i.TenantId == tenantId, cancellationToken);

        if (item == null)
            throw new NotFoundException("MealItem", request.Id);

        await _accessService.EnsureCanManageMealAsync(item.MealId, cancellationToken);

        // If FoodId is provided, update the food and recalculate
        if (request.FoodId.HasValue && request.FoodId.Value != item.FoodId)
        {
            var food = await _context.Foods
                .FirstOrDefaultAsync(f => f.Id == request.FoodId.Value
                    && !f.IsDeleted
                    && (f.TenantId == null || f.TenantId == tenantId), cancellationToken);
            if (food == null)
                throw new NotFoundException("Food", request.FoodId.Value);

            item.FoodId = request.FoodId.Value;
            item.Food = food;
        }

        item.AssignedQuantity = request.AssignedQuantity;

        // Recalculate nutritional values based on quantity
        var ratio = item.AssignedQuantity / 100.0;
        item.CalcCalories = item.Food.CaloriesPer100g * ratio;
        item.CalcProtein = item.Food.ProteinPer100g * ratio;
        item.CalcCarbs = item.Food.CarbsPer100g * ratio;
        item.CalcFats = item.Food.FatsPer100g * ratio;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
