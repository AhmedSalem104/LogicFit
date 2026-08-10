using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.WorkoutPrograms.Commands.CreateRoutineExercise;

public class CreateRoutineExerciseCommandHandler : IRequestHandler<CreateRoutineExerciseCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICoachPlanAccessService _accessService;

    public CreateRoutineExerciseCommandHandler(IApplicationDbContext context, ITenantService tenantService, ICoachPlanAccessService accessService)
    {
        _context = context;
        _tenantService = tenantService;
        _accessService = accessService;
    }

    public async Task<Guid> Handle(CreateRoutineExerciseCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var routine = await _context.ProgramRoutines
            .FirstOrDefaultAsync(r => r.Id == request.RoutineId && r.TenantId == tenantId, cancellationToken);

        if (routine == null)
            throw new NotFoundException("ProgramRoutine", request.RoutineId);

        await _accessService.EnsureCanManageRoutineAsync(request.RoutineId, cancellationToken);
        var exerciseExists = await _context.Exercises
            .AnyAsync(e => e.Id == request.ExerciseId && !e.IsDeleted && (e.TenantId == null || e.TenantId == tenantId), cancellationToken);
        if (!exerciseExists)
            throw new NotFoundException("Exercise", request.ExerciseId);

        var routineExercise = new RoutineExercise
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            RoutineId = request.RoutineId,
            ExerciseId = request.ExerciseId,
            Sets = request.Sets,
            RepsMin = request.RepsMin,
            RepsMax = request.RepsMax,
            RestSec = request.RestSec,
            TargetWeightKg = request.TargetWeightKg,
            Notes = request.Notes,
            Tempo = request.Tempo,
            SupersetGroupId = request.SupersetGroupId
        };

        _context.RoutineExercises.Add(routineExercise);
        await _context.SaveChangesAsync(cancellationToken);

        return routineExercise.Id;
    }
}
