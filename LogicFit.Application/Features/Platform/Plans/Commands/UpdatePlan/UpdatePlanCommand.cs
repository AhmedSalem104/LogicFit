using LogicFit.Application.Features.Platform.Plans.DTOs;
using LogicFit.Domain.Enums;
using MediatR;

namespace LogicFit.Application.Features.Platform.Plans.Commands.UpdatePlan;

public class UpdatePlanCommand : IRequest<PlanDto>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "EGP";
    public BillingCycle BillingCycle { get; set; } = BillingCycle.Monthly;
    public int DurationInDays { get; set; } = 30;
    public int? MaxMembers { get; set; }
    public int? MaxCoaches { get; set; }
    public int? MaxBranches { get; set; }
    public int? MaxEmployees { get; set; }
    public int? MaxStorageMB { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
    public List<string> FeatureCodes { get; set; } = new();
}
