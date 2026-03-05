using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.ClientDashboard.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.ClientDashboard.Queries.GetMyDietPlans;

public class GetMyDietPlansQueryHandler : IRequestHandler<GetMyDietPlansQuery, List<MyDietPlanDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;

    public GetMyDietPlansQueryHandler(
        IApplicationDbContext context,
        ITenantService tenantService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
    }

    public async Task<List<MyDietPlanDto>> Handle(GetMyDietPlansQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var userId = Guid.Parse(_currentUserService.UserId!);

        return await _context.DietPlans
            .Include(d => d.Coach).ThenInclude(c => c.Profile)
            .Where(d => d.TenantId == tenantId && d.ClientId == userId)
            .OrderByDescending(d => d.StartDate)
            .Select(d => new MyDietPlanDto
            {
                Id = d.Id,
                Name = d.Name,
                CoachName = d.Coach.Profile != null ? d.Coach.Profile.FullName : d.Coach.Email,
                StartDate = d.StartDate,
                EndDate = d.EndDate,
                Status = d.Status,
                TargetCalories = d.TargetCalories,
                TargetProtein = d.TargetProtein,
                TargetCarbs = d.TargetCarbs,
                TargetFats = d.TargetFats
            })
            .ToListAsync(cancellationToken);
    }
}
