using LogicFit.Domain.Enums;

namespace LogicFit.Application.Features.DietPlans.DTOs;

public class DietPlanDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid CoachId { get; set; }
    public string? CoachName { get; set; }
    public Guid ClientId { get; set; }
    public string? ClientName { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public PlanStatus Status { get; set; }
    public double? TargetCalories { get; set; }
    public double? TargetProtein { get; set; }
    public double? TargetCarbs { get; set; }
    public double? TargetFats { get; set; }
    public List<DailyMealDto> Meals { get; set; } = new();
}

public class DailyMealDto
{
    public Guid Id { get; set; }
    public Guid PlanId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
    public List<MealItemDto> Items { get; set; } = new();
}

public class MealItemDto
{
    public Guid Id { get; set; }
    public Guid MealId { get; set; }
    public int FoodId { get; set; }
    public string? FoodName { get; set; }
    public double AssignedQuantity { get; set; }
    public double CalcCalories { get; set; }
    public double CalcProtein { get; set; }
    public double CalcCarbs { get; set; }
    public double CalcFats { get; set; }
}

public class CreateDietPlanDto
{
    public Guid ClientId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public double? TargetCalories { get; set; }
    public double? TargetProtein { get; set; }
    public double? TargetCarbs { get; set; }
    public double? TargetFats { get; set; }
}

public class CreateDailyMealDto
{
    public Guid PlanId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
}

public class CreateMealItemDto
{
    public Guid MealId { get; set; }
    public int FoodId { get; set; }
    public double AssignedQuantity { get; set; }
}
