using LogicFit.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Platform.Features.Queries.GetFeatures;

public class GetFeaturesQueryHandler : IRequestHandler<GetFeaturesQuery, List<FeatureDto>>
{
    private readonly IApplicationDbContext _context;

    public GetFeaturesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<FeatureDto>> Handle(GetFeaturesQuery request, CancellationToken cancellationToken)
    {
        return await _context.Features
            .OrderBy(f => f.Code)
            .Select(f => new FeatureDto
            {
                Id = f.Id,
                Code = f.Code,
                Name = f.Name,
                NameAr = f.NameAr,
                NameEn = f.NameEn,
                Description = f.Description,
                Module = f.Module,
                IsFree = f.IsFree,
                IsActive = f.IsActive,
                SupportsQuota = f.SupportsQuota,
                Status = f.Status
            })
            .ToListAsync(cancellationToken);
    }
}
