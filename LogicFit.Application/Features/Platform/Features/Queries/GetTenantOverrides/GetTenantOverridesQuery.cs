using LogicFit.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Platform.Features.Queries.GetTenantOverrides;

public sealed class GetTenantOverridesQuery : IRequest<List<TenantFeatureOverrideDto>>
{
    public Guid? TenantId { get; init; }
}

public sealed class TenantFeatureOverrideDto
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public Guid FeatureId { get; init; }
    public string FeatureCode { get; init; } = string.Empty;
    public bool IsEnabled { get; init; }
    public int? LimitOverride { get; init; }
    public string Reason { get; init; } = string.Empty;
    public DateTime StartsAt { get; init; }
    public DateTime? EndsAt { get; init; }
    public Guid? GrantedByUserId { get; init; }
}

public sealed class GetTenantOverridesQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetTenantOverridesQuery, List<TenantFeatureOverrideDto>>
{
    public Task<List<TenantFeatureOverrideDto>> Handle(GetTenantOverridesQuery request, CancellationToken cancellationToken)
        => context.TenantFeatures.AsNoTracking()
            .Where(x => !request.TenantId.HasValue || x.TenantId == request.TenantId.Value)
            .OrderByDescending(x => x.StartsAt)
            .Select(x => new TenantFeatureOverrideDto
            {
                Id = x.Id, TenantId = x.TenantId, FeatureId = x.FeatureId,
                FeatureCode = x.Feature.Code, IsEnabled = x.IsEnabled,
                LimitOverride = x.LimitOverride, Reason = x.Reason,
                StartsAt = x.StartsAt, EndsAt = x.EndsAt,
                GrantedByUserId = x.GrantedByUserId
            }).ToListAsync(cancellationToken);
}
