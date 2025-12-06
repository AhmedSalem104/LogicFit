using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.WorkoutPrograms.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.WorkoutPrograms.Queries.GetWorkoutProgramById;

public class GetWorkoutProgramByIdQueryHandler : IRequestHandler<GetWorkoutProgramByIdQuery, WorkoutProgramDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public GetWorkoutProgramByIdQueryHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<WorkoutProgramDto?> Handle(GetWorkoutProgramByIdQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        return await _context.WorkoutPrograms
            .Include(p => p.Coach).ThenInclude(c => c.Profile)
            .Include(p => p.Client).ThenInclude(c => c.Profile)
            .Include(p => p.Routines)
                .ThenInclude(r => r.Exercises)
                    .ThenInclude(e => e.Exercise)
            .Where(p => p.Id == request.Id && p.TenantId == tenantId)
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
                EndDate = p.EndDate,
                Routines = p.Routines.Select(r => new ProgramRoutineDto
                {
                    Id = r.Id,
                    ProgramId = r.ProgramId,
                    Name = r.Name,
                    DayOfWeek = r.DayOfWeek,
                    Exercises = r.Exercises.Select(e => new RoutineExerciseDto
                    {
                        Id = e.Id,
                        RoutineId = e.RoutineId,
                        ExerciseId = e.ExerciseId,
                        ExerciseName = e.Exercise.Name,
                        Sets = e.Sets,
                        RepsMin = e.RepsMin,
                        RepsMax = e.RepsMax,
                        RestSec = e.RestSec,
                        SupersetGroupId = e.SupersetGroupId
                    }).ToList()
                }).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
