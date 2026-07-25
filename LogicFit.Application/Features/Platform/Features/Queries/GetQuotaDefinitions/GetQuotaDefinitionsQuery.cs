using LogicFit.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Platform.Features.Queries.GetQuotaDefinitions;

public sealed class GetQuotaDefinitionsQuery : IRequest<List<FeatureQuotaDefinitionDto>> { }

public sealed class FeatureQuotaDefinitionDto
{
    public Guid Id { get; init; }
    public Guid FeatureId { get; init; }
    public string FeatureCode { get; init; } = string.Empty;
    public string ResourceKey { get; init; } = string.Empty;
    public string Unit { get; init; } = string.Empty;
    public int? DefaultLimit { get; init; }
    public bool IsActive { get; init; }
}

public sealed class GetQuotaDefinitionsQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetQuotaDefinitionsQuery, List<FeatureQuotaDefinitionDto>>
{
    public Task<List<FeatureQuotaDefinitionDto>> Handle(GetQuotaDefinitionsQuery request, CancellationToken cancellationToken)
        => context.FeatureQuotaDefinitions.AsNoTracking().OrderBy(x => x.ResourceKey).Select(x => new FeatureQuotaDefinitionDto
        {
            Id = x.Id, FeatureId = x.FeatureId, FeatureCode = x.Feature.Code,
            ResourceKey = x.ResourceKey, Unit = x.Unit, DefaultLimit = x.DefaultLimit, IsActive = x.IsActive
        }).ToListAsync(cancellationToken);
}
