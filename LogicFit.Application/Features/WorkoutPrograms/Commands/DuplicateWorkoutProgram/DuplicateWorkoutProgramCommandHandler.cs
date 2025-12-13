using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.WorkoutPrograms.Commands.DuplicateWorkoutProgram;

public class DuplicateWorkoutProgramCommandHandler : IRequestHandler<DuplicateWorkoutProgramCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;

    public DuplicateWorkoutProgramCommandHandler(
        IApplicationDbContext context,
        ITenantService tenantService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
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

        // Create new program
        var newProgram = new WorkoutProgram
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CoachId = Guid.Parse(_currentUserService.UserId!),
            ClientId = request.NewClientId ?? originalProgram.ClientId,
            Name = request.NewName ?? $"{originalProgram.Name} (Copy)",
            StartDate = DateTime.UtcNow.Date,
            EndDate = originalProgram.EndDate.HasValue
                ? DateTime.UtcNow.Date.AddDays((originalProgram.EndDate.Value - originalProgram.StartDate).Days)
                : null
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
                DayOfWeek = originalRoutine.DayOfWeek
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
