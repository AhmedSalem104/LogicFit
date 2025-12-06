using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.DietPlans.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.DietPlans.Queries.GetDietPlans;

public class GetDietPlansQueryHandler : IRequestHandler<GetDietPlansQuery, List<DietPlanDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public GetDietPlansQueryHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<List<DietPlanDto>> Handle(GetDietPlansQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var query = _context.DietPlans
            .Include(p => p.Coach).ThenInclude(c => c.Profile)
            .Include(p => p.Client).ThenInclude(c => c.Profile)
            .Where(p => p.TenantId == tenantId)
            .AsQueryable();

        if (request.CoachId.HasValue)
            query = query.Where(p => p.CoachId == request.CoachId.Value);

        if (request.ClientId.HasValue)
            query = query.Where(p => p.ClientId == request.ClientId.Value);

        if (request.Status.HasValue)
            query = query.Where(p => p.Status == request.Status.Value);

        return await query
            .Select(p => new DietPlanDto
            {
                Id = p.Id,
                TenantId = p.TenantId,
                CoachId = p.CoachId,
                CoachName = p.Coach.Profile != null ? p.Coach.Profile.FullName : p.Coach.Email,
                ClientId = p.ClientId,
                ClientName = p.Client.Profile != null ? p.Client.Profile.FullName : p.Client.Email,
                Name = p.Name,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                Status = p.Status,
                TargetCalories = p.TargetCalories,
                TargetProtein = p.TargetProtein,
                TargetCarbs = p.TargetCarbs,
                TargetFats = p.TargetFats
            })
            .ToListAsync(cancellationToken);
    }
}
