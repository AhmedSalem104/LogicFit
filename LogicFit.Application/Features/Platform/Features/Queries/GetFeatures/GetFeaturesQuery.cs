using MediatR;

namespace LogicFit.Application.Features.Platform.Features.Queries.GetFeatures;

public class GetFeaturesQuery : IRequest<List<FeatureDto>>
{
}

public class FeatureDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public string? NameEn { get; set; }
    public string? Description { get; set; }
    public string? Module { get; set; }
    public bool IsFree { get; set; }
    public bool IsActive { get; set; }
    public bool SupportsQuota { get; set; }
    public LogicFit.Domain.Enums.FeatureLifecycleStatus Status { get; set; }
}
