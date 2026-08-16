using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.WorkoutPrograms.Commands.DuplicateWorkoutProgram;

public class DuplicateWorkoutProgramCommandHandler : IRequestHandler<DuplicateWorkoutProgramCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICoachPlanAccessService _accessService;

    public DuplicateWorkoutProgramCommandHandler(
        IApplicationDbContext context,
        ITenantService tenantService,
        ICurrentUserService currentUserService,
        ICoachPlanAccessService accessService)
    {
        _context = context;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
        _accessService = accessService;
    }

    public async Task<Guid> Handle(DuplicateWorkoutProgramCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var originalProgram = await _context.WorkoutPrograms
            .Include(p => p.Routines)
                .ThenInclude(r => r.Exercises)
            .FirstOrDefaultAsync(p => p.Id == request.Id && p.TenantId == tenantId, cancellationToken);

        if (originalProgram == null)
            throw new NotFoundException("WorkoutProgram", request.Id);

        await _accessService.EnsureCanManageWorkoutProgramAsync(request.Id, cancellationToken);
        await _accessService.EnsureCanManageClientAsync(request.NewClientId ?? originalProgram.ClientId, cancellationToken);

        // Create new program
        var newProgram = new WorkoutProgram
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CoachId = Guid.Parse(_currentUserService.UserId!),
            ClientId = request.NewClientId ?? originalProgram.ClientId,
            Name = request.NewName ?? $"{originalProgram.Name} (Copy)",
            Description = originalProgram.Description,
            Goal = originalProgram.Goal,
            Difficulty = originalProgram.Difficulty,
            DaysPerWeek = originalProgram.DaysPerWeek,
            Status = PlanStatus.Draft,
            StartDate = DateTime.UtcNow.Date,
            EndDate = originalProgram.EndDate.HasValue
                ? DateTime.UtcNow.Date.AddDays((originalProgram.EndDate.Value - originalProgram.StartDate).Days)
                : null,
            Notes = originalProgram.Notes
        };

        // Clone routines
        foreach (var originalRoutine in originalProgram.Routines)
        {
            var newRoutine = new ProgramRoutine
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                ProgramId = newProgram.Id,
                Name = originalRoutine.Name,
                DayOfWeek = originalRoutine.DayOfWeek,
                Notes = originalRoutine.Notes
            };

            // Clone routine exercises
            foreach (var originalExercise in originalRoutine.Exercises)
            {
                var newExercise = new RoutineExercise
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    RoutineId = newRoutine.Id,
                    ExerciseId = originalExercise.ExerciseId,
                    Sets = originalExercise.Sets,
                    RepsMin = originalExercise.RepsMin,
                    RepsMax = originalExercise.RepsMax,
                    RestSec = originalExercise.RestSec,
                    TargetWeightKg = originalExercise.TargetWeightKg,
                    Notes = originalExercise.Notes,
                    Tempo = originalExercise.Tempo,
                    SupersetGroupId = originalExercise.SupersetGroupId
                };
                newRoutine.Exercises.Add(newExercise);
            }

            newProgram.Routines.Add(newRoutine);
        }

        _context.WorkoutPrograms.Add(newProgram);
        await _context.SaveChangesAsync(cancellationToken);

        return newProgram.Id;
    }
}
