using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.WorkoutPrograms.Commands.DeleteWorkoutProgram;

public class DeleteWorkoutProgramCommandHandler : IRequestHandler<DeleteWorkoutProgramCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICoachPlanAccessService _accessService;

    public DeleteWorkoutProgramCommandHandler(IApplicationDbContext context, ITenantService tenantService, ICoachPlanAccessService accessService)
    {
        _context = context;
        _tenantService = tenantService;
        _accessService = accessService;
    }

    public async Task<bool> Handle(DeleteWorkoutProgramCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var program = await _context.WorkoutPrograms
            .FirstOrDefaultAsync(p => p.Id == request.Id && p.TenantId == tenantId, cancellationToken);

        if (program == null)
            throw new NotFoundException("WorkoutProgram", request.Id);

        await _accessService.EnsureCanManageWorkoutProgramAsync(request.Id, cancellationToken);

        _context.WorkoutPrograms.Remove(program);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
