using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.WorkoutPrograms.DTOs;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.WorkoutPrograms.Commands.UpdateWorkoutProgram;

public class UpdateWorkoutProgramCommandHandler : IRequestHandler<UpdateWorkoutProgramCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICoachPlanAccessService _accessService;

    public UpdateWorkoutProgramCommandHandler(
        IApplicationDbContext context,
        ITenantService tenantService,
        ICoachPlanAccessService accessService)
    {
        _context = context;
        _tenantService = tenantService;
        _accessService = accessService;
    }

    public async Task<bool> Handle(UpdateWorkoutProgramCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var program = await _context.WorkoutPrograms
            .Include(p => p.Routines)
                .ThenInclude(r => r.Exercises)
            .FirstOrDefaultAsync(p => p.Id == request.Id && p.TenantId == tenantId, cancellationToken);

        if (program == null)
            throw new NotFoundException("WorkoutProgram", request.Id);

        await _accessService.EnsureCanManageWorkoutProgramAsync(request.Id, cancellationToken);
        if (request.ClientId.HasValue && request.ClientId.Value != program.ClientId)
        {
            await _accessService.EnsureCanManageClientAsync(request.ClientId.Value, cancellationToken);
            program.ClientId = request.ClientId.Value;
        }
        if (request.Routines != null)
        {
            await EnsureExercisesBelongToTenantAsync(request.Routines, tenantId, cancellationToken);
            ReconcileRoutines(program, request.Routines, tenantId);
        }

        program.Name = request.Name;
        program.Description = request.Description;
        program.Goal = request.Goal;
        program.Difficulty = request.Difficulty;
        program.DaysPerWeek = request.DaysPerWeek;
        if (request.Status.HasValue)
            program.Status = request.Status.Value;
        program.StartDate = request.StartDate;
        program.EndDate = request.EndDate;

        await using var transaction = await _context.BeginTransactionAsync(cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return true;
    }

    private void ReconcileRoutines(WorkoutProgram program, IReadOnlyCollection<WorkoutRoutineInputDto> requested, Guid tenantId)
    {
        var requestedIds = requested.Where(r => r.Id.HasValue).Select(r => r.Id!.Value).ToHashSet();
        foreach (var existingRoutine in program.Routines.Where(r => !requestedIds.Contains(r.Id)).ToList())
        {
            _context.RoutineExercises.RemoveRange(existingRoutine.Exercises);
            _context.ProgramRoutines.Remove(existingRoutine);
        }

        foreach (var routineInput in requested)
        {
            ProgramRoutine routine;
            if (routineInput.Id.HasValue)
            {
                routine = program.Routines.FirstOrDefault(r => r.Id == routineInput.Id.Value)
                    ?? throw new NotFoundException("ProgramRoutine", routineInput.Id.Value);
                routine.Name = routineInput.Name;
                routine.DayOfWeek = routineInput.DayOfWeek;
            }
            else
            {
                routine = new ProgramRoutine
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    ProgramId = program.Id,
                    Name = routineInput.Name,
                    DayOfWeek = routineInput.DayOfWeek
                };
                program.Routines.Add(routine);
            }

            var requestedExerciseIds = routineInput.Exercises
                .Where(e => e.Id.HasValue)
                .Select(e => e.Id!.Value)
                .ToHashSet();
            foreach (var existingExercise in routine.Exercises
                .Where(e => !requestedExerciseIds.Contains(e.Id))
                .ToList())
            {
                _context.RoutineExercises.Remove(existingExercise);
            }

            foreach (var exerciseInput in routineInput.Exercises)
            {
                RoutineExercise exercise;
                if (exerciseInput.Id.HasValue)
                {
                    exercise = routine.Exercises.FirstOrDefault(e => e.Id == exerciseInput.Id.Value)
                        ?? throw new NotFoundException("RoutineExercise", exerciseInput.Id.Value);
                }
                else
                {
                    exercise = new RoutineExercise
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        RoutineId = routine.Id
                    };
                    routine.Exercises.Add(exercise);
                }

                exercise.ExerciseId = exerciseInput.ExerciseId;
                exercise.Sets = exerciseInput.Sets;
                exercise.RepsMin = exerciseInput.RepsMin;
                exercise.RepsMax = exerciseInput.RepsMax;
                exercise.RestSec = exerciseInput.RestSec;
                exercise.TargetWeightKg = exerciseInput.TargetWeightKg;
                exercise.Notes = exerciseInput.Notes;
                exercise.Tempo = exerciseInput.Tempo;
                exercise.SupersetGroupId = exerciseInput.SupersetGroupId;
            }
        }
    }

    private async Task EnsureExercisesBelongToTenantAsync(
        IEnumerable<WorkoutRoutineInputDto> routines,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var exerciseIds = routines.SelectMany(r => r.Exercises).Select(e => e.ExerciseId).Distinct().ToList();
        if (exerciseIds.Count == 0)
            return;

        var availableIds = await _context.Exercises
            .Where(e => exerciseIds.Contains(e.Id) && !e.IsDeleted && (e.TenantId == null || e.TenantId == tenantId))
            .Select(e => e.Id)
            .ToListAsync(cancellationToken);
        var missingId = exerciseIds.FirstOrDefault(id => !availableIds.Contains(id));
        if (missingId != 0)
            throw new NotFoundException("Exercise", missingId);
    }
}
