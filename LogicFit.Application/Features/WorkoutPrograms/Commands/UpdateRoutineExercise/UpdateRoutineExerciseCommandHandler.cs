using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.WorkoutPrograms.Commands.UpdateRoutineExercise;

public class UpdateRoutineExerciseCommandHandler : IRequestHandler<UpdateRoutineExerciseCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICoachPlanAccessService _accessService;

    public UpdateRoutineExerciseCommandHandler(
        IApplicationDbContext context,
        ITenantService tenantService,
        ICoachPlanAccessService accessService)
    {
        _context = context;
        _tenantService = tenantService;
        _accessService = accessService;
    }

    public async Task<bool> Handle(UpdateRoutineExerciseCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var routineExercise = await _context.RoutineExercises
            .Include(e => e.Routine)
            .FirstOrDefaultAsync(e => e.Id == request.Id && e.TenantId == tenantId, cancellationToken);

        if (routineExercise == null)
            throw new NotFoundException("RoutineExercise", request.Id);

        await _accessService.EnsureCanManageRoutineAsync(routineExercise.RoutineId, cancellationToken);

        // If ExerciseId is provided, update it
        if (request.ExerciseId.HasValue)
        {
            var exercise = await _context.Exercises
                .FirstOrDefaultAsync(e => e.Id == request.ExerciseId.Value
                    && !e.IsDeleted
                    && (e.TenantId == null || e.TenantId == tenantId), cancellationToken);
            if (exercise == null)
                throw new NotFoundException("Exercise", request.ExerciseId.Value);

            routineExercise.ExerciseId = request.ExerciseId.Value;
        }

        routineExercise.Sets = request.Sets;
        routineExercise.RepsMin = request.RepsMin;
        routineExercise.RepsMax = request.RepsMax;
        routineExercise.RestSec = request.RestSec;
        routineExercise.TargetWeightKg = request.TargetWeightKg;
        routineExercise.Notes = request.Notes;
        routineExercise.Tempo = request.Tempo;
        routineExercise.SupersetGroupId = request.SupersetGroupId;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
