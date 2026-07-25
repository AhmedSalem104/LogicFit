using LogicFit.Domain.Enums;

namespace LogicFit.Application.Features.Platform.Plans.DTOs;

public class PlanDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "EGP";
    public BillingCycle BillingCycle { get; set; }
    public int DurationInDays { get; set; }
    public int? MaxMembers { get; set; }
    public int? MaxCoaches { get; set; }
    public int? MaxBranches { get; set; }
    public int? MaxEmployees { get; set; }
    public int? MaxStorageMB { get; set; }
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
    public List<string> Features { get; set; } = new();
    public Dictionary<string, int?> FeatureLimits { get; set; } = new();
}
