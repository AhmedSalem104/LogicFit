using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.DietPlans.Commands.DuplicateDietPlan;

public class DuplicateDietPlanCommandHandler : IRequestHandler<DuplicateDietPlanCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;

    public DuplicateDietPlanCommandHandler(
        IApplicationDbContext context,
        ITenantService tenantService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(DuplicateDietPlanCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var originalPlan = await _context.DietPlans
            .Include(p => p.Meals)
                .ThenInclude(m => m.Items)
            .FirstOrDefaultAsync(p => p.Id == request.Id && p.TenantId == tenantId, cancellationToken);

        if (originalPlan == null)
            throw new NotFoundException("DietPlan", request.Id);

        // Create new plan
        var newPlan = new DietPlan
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CoachId = Guid.Parse(_currentUserService.UserId!),
            ClientId = request.NewClientId ?? originalPlan.ClientId,
            Name = request.NewName ?? $"{originalPlan.Name} (Copy)",
            StartDate = DateTime.UtcNow.Date,
            EndDate = originalPlan.EndDate.HasValue
                ? DateTime.UtcNow.Date.AddDays((originalPlan.EndDate.Value - originalPlan.StartDate).Days)
                : null,
            Status = PlanStatus.Draft,
            TargetCalories = originalPlan.TargetCalories,
            TargetProtein = originalPlan.TargetProtein,
            TargetCarbs = originalPlan.TargetCarbs,
            TargetFats = originalPlan.TargetFats
        };

        // Clone meals
        foreach (var originalMeal in originalPlan.Meals)
        {
            var newMeal = new DailyMeal
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                PlanId = newPlan.Id,
                Name = originalMeal.Name,
                OrderIndex = originalMeal.OrderIndex
            };

            // Clone meal items
            foreach (var originalItem in originalMeal.Items)
            {
                var newItem = new MealItem
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    MealId = newMeal.Id,
                    FoodId = originalItem.FoodId,
                    AssignedQuantity = originalItem.AssignedQuantity,
                    CalcCalories = originalItem.CalcCalories,
                    CalcProtein = originalItem.CalcProtein,
                    CalcCarbs = originalItem.CalcCarbs,
                    CalcFats = originalItem.CalcFats
                };
                newMeal.Items.Add(newItem);
            }

            newPlan.Meals.Add(newMeal);
        }

        _context.DietPlans.Add(newPlan);
        await _context.SaveChangesAsync(cancellationToken);

        return newPlan.Id;
    }
}
