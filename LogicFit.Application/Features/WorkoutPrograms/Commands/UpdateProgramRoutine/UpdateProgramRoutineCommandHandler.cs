using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.WorkoutPrograms.Commands.UpdateProgramRoutine;

public class UpdateProgramRoutineCommandHandler : IRequestHandler<UpdateProgramRoutineCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICoachPlanAccessService _accessService;

    public UpdateProgramRoutineCommandHandler(
        IApplicationDbContext context,
        ITenantService tenantService,
        ICoachPlanAccessService accessService)
    {
        _context = context;
        _tenantService = tenantService;
        _accessService = accessService;
    }

    public async Task<bool> Handle(UpdateProgramRoutineCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var routine = await _context.ProgramRoutines
            .FirstOrDefaultAsync(r => r.Id == request.Id && r.TenantId == tenantId, cancellationToken);

        if (routine == null)
            throw new NotFoundException("ProgramRoutine", request.Id);

        await _accessService.EnsureCanManageRoutineAsync(request.Id, cancellationToken);

        routine.Name = request.Name;
        routine.DayOfWeek = request.DayOfWeek;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
