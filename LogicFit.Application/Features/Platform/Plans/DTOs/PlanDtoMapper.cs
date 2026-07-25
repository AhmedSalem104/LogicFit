using LogicFit.Domain.Entities;

namespace LogicFit.Application.Features.Platform.Plans.DTOs;

/// <summary>
/// Maps an already materialized plan graph to the platform administration contract.
/// Feature limits deliberately stay out of the EF projection because Dictionary materialization
/// is a CLR operation and is not translatable by SQL Server.
/// </summary>
public static class PlanDtoMapper
{
    public static PlanDto Map(Plan plan) => new()
    {
        Id = plan.Id,
        Name = plan.Name,
        Description = plan.Description,
        Price = plan.Price,
        Currency = plan.Currency,
        BillingCycle = plan.BillingCycle,
        DurationInDays = plan.DurationInDays,
        MaxMembers = plan.MaxMembers,
        MaxCoaches = plan.MaxCoaches,
        MaxBranches = plan.MaxBranches,
        MaxEmployees = plan.MaxEmployees,
        MaxStorageMB = plan.MaxStorageMB,
        IsActive = plan.IsActive,
        DisplayOrder = plan.DisplayOrder,
        Features = plan.PlanFeatures.Select(planFeature => planFeature.Feature.Code).ToList(),
        FeatureLimits = plan.PlanFeatures.ToDictionary(planFeature => planFeature.Feature.Code, planFeature => planFeature.LimitValue)
    };
}
