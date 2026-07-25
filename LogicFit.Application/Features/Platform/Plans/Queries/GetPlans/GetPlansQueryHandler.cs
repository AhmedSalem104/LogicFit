using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Platform.Plans.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Platform.Plans.Queries.GetPlans;

public class GetPlansQueryHandler : IRequestHandler<GetPlansQuery, List<PlanDto>>
{
    private readonly IApplicationDbContext _context;

    public GetPlansQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<PlanDto>> Handle(GetPlansQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Plans.AsQueryable();
        if (request.ActiveOnly)
        {
            query = query.Where(p => p.IsActive);
        }

        var plans = await query
            .AsNoTracking()
            .Include(p => p.PlanFeatures)
            .ThenInclude(planFeature => planFeature.Feature)
            .OrderBy(p => p.DisplayOrder)
            .ToListAsync(cancellationToken);

        return plans.Select(PlanDtoMapper.Map).ToList();
    }
}
