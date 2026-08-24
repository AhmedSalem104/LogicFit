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
            .Include(p => p.Routines)
                .ThenInclude(r => r.Exercises)
            .FirstOrDefaultAsync(p => p.Id == request.Id && p.TenantId == tenantId, cancellationToken);

        if (program == null)
            throw new NotFoundException("WorkoutProgram", request.Id);

        await _accessService.EnsureCanManageWorkoutProgramAsync(request.Id, cancellationToken);

        program.IsDeleted = true;
        program.DeletedAt = DateTime.UtcNow;
        foreach (var routine in program.Routines)
        {
            routine.IsDeleted = true;
            routine.DeletedAt = DateTime.UtcNow;
            foreach (var exercise in routine.Exercises)
            {
                exercise.IsDeleted = true;
                exercise.DeletedAt = DateTime.UtcNow;
            }
        }
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
