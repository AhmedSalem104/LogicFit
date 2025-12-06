using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.WorkoutPrograms.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.WorkoutPrograms.Queries.GetWorkoutPrograms;

public class GetWorkoutProgramsQueryHandler : IRequestHandler<GetWorkoutProgramsQuery, List<WorkoutProgramDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public GetWorkoutProgramsQueryHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<List<WorkoutProgramDto>> Handle(GetWorkoutProgramsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var query = _context.WorkoutPrograms
            .Include(p => p.Coach).ThenInclude(c => c.Profile)
            .Include(p => p.Client).ThenInclude(c => c.Profile)
            .Where(p => p.TenantId == tenantId)
            .AsQueryable();

        if (request.CoachId.HasValue)
            query = query.Where(p => p.CoachId == request.CoachId.Value);

        if (request.ClientId.HasValue)
            query = query.Where(p => p.ClientId == request.ClientId.Value);

        return await query
            .Select(p => new WorkoutProgramDto
            {
                Id = p.Id,
                TenantId = p.TenantId,
                CoachId = p.CoachId,
                CoachName = p.Coach.Profile != null ? p.Coach.Profile.FullName : p.Coach.Email,
                ClientId = p.ClientId,
                ClientName = p.Client.Profile != null ? p.Client.Profile.FullName : p.Client.Email,
                Name = p.Name,
                StartDate = p.StartDate,
                EndDate = p.EndDate
            })
            .ToListAsync(cancellationToken);
    }
}
