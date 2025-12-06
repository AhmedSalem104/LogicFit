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

    public CreateRoutineExerciseCommandHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<Guid> Handle(CreateRoutineExerciseCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var routine = await _context.ProgramRoutines
            .FirstOrDefaultAsync(r => r.Id == request.RoutineId && r.TenantId == tenantId, cancellationToken);

        if (routine == null)
            throw new NotFoundException("ProgramRoutine", request.RoutineId);

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
            SupersetGroupId = request.SupersetGroupId
        };

        _context.RoutineExercises.Add(routineExercise);
        await _context.SaveChangesAsync(cancellationToken);

        return routineExercise.Id;
    }
}
