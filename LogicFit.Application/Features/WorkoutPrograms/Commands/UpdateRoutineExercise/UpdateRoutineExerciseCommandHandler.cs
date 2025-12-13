using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.WorkoutPrograms.Commands.UpdateRoutineExercise;

public class UpdateRoutineExerciseCommandHandler : IRequestHandler<UpdateRoutineExerciseCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public UpdateRoutineExerciseCommandHandler(
        IApplicationDbContext context,
        ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<bool> Handle(UpdateRoutineExerciseCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var routineExercise = await _context.RoutineExercises
            .FirstOrDefaultAsync(e => e.Id == request.Id && e.TenantId == tenantId, cancellationToken);

        if (routineExercise == null)
            throw new NotFoundException("RoutineExercise", request.Id);

        // If ExerciseId is provided, update it
        if (request.ExerciseId.HasValue)
        {
            var exercise = await _context.Exercises.FindAsync(new object[] { request.ExerciseId.Value }, cancellationToken);
            if (exercise == null)
                throw new NotFoundException("Exercise", request.ExerciseId.Value);

            routineExercise.ExerciseId = request.ExerciseId.Value;
        }

        routineExercise.Sets = request.Sets;
        routineExercise.RepsMin = request.RepsMin;
        routineExercise.RepsMax = request.RepsMax;
        routineExercise.RestSec = request.RestSec;
        routineExercise.SupersetGroupId = request.SupersetGroupId;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
