using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.WorkoutPrograms.Commands.DeleteRoutineExercise;

public class DeleteRoutineExerciseCommandHandler : IRequestHandler<DeleteRoutineExerciseCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICoachPlanAccessService _accessService;

    public DeleteRoutineExerciseCommandHandler(
        IApplicationDbContext context,
        ITenantService tenantService,
        ICoachPlanAccessService accessService)
    {
        _context = context;
        _tenantService = tenantService;
        _accessService = accessService;
    }

    public async Task<bool> Handle(DeleteRoutineExerciseCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var routineExercise = await _context.RoutineExercises
            .FirstOrDefaultAsync(e => e.Id == request.Id && e.TenantId == tenantId, cancellationToken);

        if (routineExercise == null)
            throw new NotFoundException("RoutineExercise", request.Id);

        await _accessService.EnsureCanManageRoutineAsync(routineExercise.RoutineId, cancellationToken);

        _context.RoutineExercises.Remove(routineExercise);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
