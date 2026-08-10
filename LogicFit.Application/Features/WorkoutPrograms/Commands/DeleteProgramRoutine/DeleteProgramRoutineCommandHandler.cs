using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.WorkoutPrograms.Commands.DeleteProgramRoutine;

public class DeleteProgramRoutineCommandHandler : IRequestHandler<DeleteProgramRoutineCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICoachPlanAccessService _accessService;

    public DeleteProgramRoutineCommandHandler(
        IApplicationDbContext context,
        ITenantService tenantService,
        ICoachPlanAccessService accessService)
    {
        _context = context;
        _tenantService = tenantService;
        _accessService = accessService;
    }

    public async Task<bool> Handle(DeleteProgramRoutineCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var routine = await _context.ProgramRoutines
            .Include(r => r.Exercises)
            .FirstOrDefaultAsync(r => r.Id == request.Id && r.TenantId == tenantId, cancellationToken);

        if (routine == null)
            throw new NotFoundException("ProgramRoutine", request.Id);

        await _accessService.EnsureCanManageRoutineAsync(request.Id, cancellationToken);

        // Delete all exercises first
        _context.RoutineExercises.RemoveRange(routine.Exercises);

        // Then delete the routine
        _context.ProgramRoutines.Remove(routine);

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
