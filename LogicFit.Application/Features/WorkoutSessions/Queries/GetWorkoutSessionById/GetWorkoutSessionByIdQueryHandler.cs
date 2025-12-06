using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.WorkoutSessions.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.WorkoutSessions.Queries.GetWorkoutSessionById;

public class GetWorkoutSessionByIdQueryHandler : IRequestHandler<GetWorkoutSessionByIdQuery, WorkoutSessionDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public GetWorkoutSessionByIdQueryHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<WorkoutSessionDto?> Handle(GetWorkoutSessionByIdQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        return await _context.WorkoutSessions
            .Include(s => s.Client).ThenInclude(c => c.Profile)
            .Include(s => s.Routine)
            .Include(s => s.Sets).ThenInclude(st => st.Exercise)
            .Where(s => s.Id == request.Id && s.TenantId == tenantId)
            .Select(s => new WorkoutSessionDto
            {
                Id = s.Id,
                TenantId = s.TenantId,
                ClientId = s.ClientId,
                ClientName = s.Client.Profile != null ? s.Client.Profile.FullName : s.Client.Email,
                RoutineId = s.RoutineId,
                RoutineName = s.Routine.Name,
                StartedAt = s.StartedAt,
                EndedAt = s.EndedAt,
                TotalVolumLifted = s.TotalVolumLifted,
                Notes = s.Notes,
                Sets = s.Sets.Select(st => new SessionSetDto
                {
                    Id = st.Id,
                    SessionId = st.SessionId,
                    ExerciseId = st.ExerciseId,
                    ExerciseName = st.Exercise.Name,
                    SetNumber = st.SetNumber,
                    WeightKg = st.WeightKg,
                    Reps = st.Reps,
                    Rpe = st.Rpe,
                    VolumeLoad = st.VolumeLoad,
                    IsPr = st.IsPr
                }).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
