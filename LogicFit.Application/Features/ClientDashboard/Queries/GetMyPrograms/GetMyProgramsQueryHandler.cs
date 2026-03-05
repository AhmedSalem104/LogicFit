using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.ClientDashboard.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.ClientDashboard.Queries.GetMyPrograms;

public class GetMyProgramsQueryHandler : IRequestHandler<GetMyProgramsQuery, List<MyWorkoutProgramDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;

    public GetMyProgramsQueryHandler(
        IApplicationDbContext context,
        ITenantService tenantService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
    }

    public async Task<List<MyWorkoutProgramDto>> Handle(GetMyProgramsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var userId = Guid.Parse(_currentUserService.UserId!);

        return await _context.WorkoutPrograms
            .Include(p => p.Coach).ThenInclude(c => c.Profile)
            .Where(p => p.TenantId == tenantId && p.ClientId == userId)
            .OrderByDescending(p => p.StartDate)
            .Select(p => new MyWorkoutProgramDto
            {
                Id = p.Id,
                Name = p.Name,
                CoachName = p.Coach.Profile != null ? p.Coach.Profile.FullName : p.Coach.Email,
                StartDate = p.StartDate,
                EndDate = p.EndDate
            })
            .ToListAsync(cancellationToken);
    }
}
