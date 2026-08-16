using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.WorkoutPrograms.DTOs;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.WorkoutPrograms.Commands.CreateWorkoutProgram;

public class CreateWorkoutProgramCommandHandler : IRequestHandler<CreateWorkoutProgramCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICoachPlanAccessService _accessService;

    public CreateWorkoutProgramCommandHandler(
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

    public async Task<Guid> Handle(CreateWorkoutProgramCommand request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(_currentUserService.UserId, out var currentUserId))
            throw new ForbiddenException("An authenticated coach is required.");

        var tenantId = _tenantService.GetCurrentTenantId();
        await _accessService.EnsureCanManageClientAsync(request.ClientId, cancellationToken);
        await EnsureExercisesBelongToTenantAsync(request.Routines, tenantId, cancellationToken);

        var program = new WorkoutProgram
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CoachId = currentUserId,
            ClientId = request.ClientId,
            Name = request.Name,
            Description = request.Description,
            Goal = request.Goal,
            Difficulty = request.Difficulty,
            DaysPerWeek = request.DaysPerWeek,
            Status = request.Status ?? PlanStatus.Active,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Notes = request.Notes?.Trim()
        };

        foreach (var routineInput in request.Routines)
        {
            var routine = new ProgramRoutine
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Name = routineInput.Name,
                DayOfWeek = routineInput.DayOfWeek,
                Notes = routineInput.Notes?.Trim()
            };

            foreach (var exerciseInput in routineInput.Exercises)
            {
                routine.Exercises.Add(new RoutineExercise
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    ExerciseId = exerciseInput.ExerciseId,
                    Sets = exerciseInput.Sets,
                    RepsMin = exerciseInput.RepsMin,
                    RepsMax = exerciseInput.RepsMax,
                    RestSec = exerciseInput.RestSec,
                    TargetWeightKg = exerciseInput.TargetWeightKg,
                    Notes = exerciseInput.Notes,
                    Tempo = exerciseInput.Tempo,
                    SupersetGroupId = exerciseInput.SupersetGroupId
                });
            }

            program.Routines.Add(routine);
        }

        await using var transaction = await _context.BeginTransactionAsync(cancellationToken);
        _context.WorkoutPrograms.Add(program);
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return program.Id;
    }

    private async Task EnsureExercisesBelongToTenantAsync(
        IEnumerable<WorkoutRoutineInputDto> routines,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var exerciseIds = routines
            .SelectMany(r => r.Exercises)
            .Select(e => e.ExerciseId)
            .Distinct()
            .ToList();

        if (exerciseIds.Count == 0)
            return;

        var availableIds = await _context.Exercises
            .Where(e => exerciseIds.Contains(e.Id)
                && !e.IsDeleted
                && (e.TenantId == null || e.TenantId == tenantId))
            .Select(e => e.Id)
            .ToListAsync(cancellationToken);

        var missingId = exerciseIds.FirstOrDefault(id => !availableIds.Contains(id));
        if (missingId != 0)
            throw new NotFoundException("Exercise", missingId);
    }
}
