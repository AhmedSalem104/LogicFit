using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.DietPlans.Commands.CreateMealItem;

public class CreateMealItemCommandHandler : IRequestHandler<CreateMealItemCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public CreateMealItemCommandHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<Guid> Handle(CreateMealItemCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var meal = await _context.DailyMeals
            .FirstOrDefaultAsync(m => m.Id == request.MealId && m.TenantId == tenantId, cancellationToken);

        if (meal == null)
            throw new NotFoundException("DailyMeal", request.MealId);

        var food = await _context.Foods
            .FirstOrDefaultAsync(f => f.Id == request.FoodId, cancellationToken);

        if (food == null)
            throw new NotFoundException("Food", request.FoodId);

        var quantity = request.AssignedQuantity / 100;
        var item = new MealItem
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            MealId = request.MealId,
            FoodId = request.FoodId,
            AssignedQuantity = request.AssignedQuantity,
            CalcCalories = food.CaloriesPer100g * quantity,
            CalcProtein = food.ProteinPer100g * quantity,
            CalcCarbs = food.CarbsPer100g * quantity,
            CalcFats = food.FatsPer100g * quantity
        };

        _context.MealItems.Add(item);
        await _context.SaveChangesAsync(cancellationToken);

        return item.Id;
    }
}
