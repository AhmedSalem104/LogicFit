using LogicFit.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Platform.Features.Queries.GetFeatureDependencies;

public sealed class GetFeatureDependenciesQuery : IRequest<List<FeatureDependencyDto>>;
public sealed record FeatureDependencyDto(Guid Id, Guid FeatureId, string FeatureCode, Guid DependsOnFeatureId, string DependsOnFeatureCode);
public sealed class GetFeatureDependenciesHandler(IApplicationDbContext context) : IRequestHandler<GetFeatureDependenciesQuery, List<FeatureDependencyDto>>
{
    public Task<List<FeatureDependencyDto>> Handle(GetFeatureDependenciesQuery request, CancellationToken cancellationToken) => context.FeatureDependencies.AsNoTracking().Select(x => new FeatureDependencyDto(x.Id, x.FeatureId, x.Feature.Code, x.DependsOnFeatureId, x.DependsOnFeature.Code)).ToListAsync(cancellationToken);
}
