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

        return await query
            .OrderBy(p => p.DisplayOrder)
            .Select(p => new PlanDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                Currency = p.Currency,
                BillingCycle = p.BillingCycle,
                DurationInDays = p.DurationInDays,
                MaxMembers = p.MaxMembers,
                MaxCoaches = p.MaxCoaches,
                MaxBranches = p.MaxBranches,
                MaxEmployees = p.MaxEmployees,
                MaxStorageMB = p.MaxStorageMB,
                IsActive = p.IsActive,
                DisplayOrder = p.DisplayOrder,
                Features = p.PlanFeatures.Select(pf => pf.Feature.Code).ToList()
            })
            .ToListAsync(cancellationToken);
    }
}
