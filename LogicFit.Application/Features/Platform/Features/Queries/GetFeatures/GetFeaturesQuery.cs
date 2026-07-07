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
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}
