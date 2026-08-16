using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.WorkoutPrograms.DTOs;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.WorkoutPrograms.Queries.GetWorkoutProgramById;

public class GetWorkoutProgramByIdQueryHandler : IRequestHandler<GetWorkoutProgramByIdQuery, WorkoutProgramDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;

    public GetWorkoutProgramByIdQueryHandler(IApplicationDbContext context, ITenantService tenantService, ICurrentUserService currentUserService)
    {
        _context = context;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
    }

    public async Task<WorkoutProgramDto?> Handle(GetWorkoutProgramByIdQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        if (!Guid.TryParse(_currentUserService.UserId, out var currentUserId))
            throw new ForbiddenException("An authenticated workspace user is required.");
        var role = await _context.Users.Where(u => u.Id == currentUserId && u.TenantId == tenantId && u.IsActive)
            .Select(u => (UserRole?)u.Role).FirstOrDefaultAsync(cancellationToken);
        if (!role.HasValue)
            throw new ForbiddenException("The authenticated user is not active in this workspace.");

        return await _context.WorkoutPrograms
            .Include(p => p.Coach).ThenInclude(c => c.Profile)
            .Include(p => p.Client).ThenInclude(c => c.Profile)
            .Include(p => p.Routines)
                .ThenInclude(r => r.Exercises)
                    .ThenInclude(e => e.Exercise)
            .Where(p => p.Id == request.Id && p.TenantId == tenantId
                && ((role == UserRole.Client && p.ClientId == currentUserId && p.Status == PlanStatus.Active)
                    || (role == UserRole.Owner || role == UserRole.Manager || role == UserRole.FreelanceOwner)
                    || ((role == UserRole.Coach || role == UserRole.Trainer || role == UserRole.FreelanceCoach)
                        && _context.CoachClients.Any(cc => cc.TenantId == tenantId
                            && cc.CoachId == currentUserId
                            && cc.ClientId == p.ClientId
                            && cc.IsActive
                            && cc.UnassignedAt == null))))
            .Select(p => new WorkoutProgramDto
            {
                Id = p.Id,
                TenantId = p.TenantId,
                CoachId = p.CoachId,
                CoachName = p.Coach.Profile != null ? p.Coach.Profile.FullName : p.Coach.Email,
                ClientId = p.ClientId,
                ClientName = p.Client.Profile != null ? p.Client.Profile.FullName : p.Client.Email,
                Name = p.Name,
                Description = p.Description,
                Goal = p.Goal,
                Difficulty = p.Difficulty,
                DaysPerWeek = p.DaysPerWeek,
                Status = p.Status,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                Notes = p.Notes,
                Version = p.Version,
                Routines = p.Routines.Select(r => new ProgramRoutineDto
                {
                    Id = r.Id,
                    ProgramId = r.ProgramId,
                    Name = r.Name,
                    DayOfWeek = r.DayOfWeek,
                    Notes = r.Notes,
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
                        TargetWeightKg = e.TargetWeightKg,
                        Notes = e.Notes,
                        Tempo = e.Tempo,
                        SupersetGroupId = e.SupersetGroupId
                    }).ToList()
                }).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
