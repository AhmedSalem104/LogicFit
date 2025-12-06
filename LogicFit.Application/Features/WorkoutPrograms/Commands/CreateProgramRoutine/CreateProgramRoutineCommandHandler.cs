using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.WorkoutPrograms.Commands.CreateProgramRoutine;

public class CreateProgramRoutineCommandHandler : IRequestHandler<CreateProgramRoutineCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public CreateProgramRoutineCommandHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<Guid> Handle(CreateProgramRoutineCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var program = await _context.WorkoutPrograms
            .FirstOrDefaultAsync(p => p.Id == request.ProgramId && p.TenantId == tenantId, cancellationToken);

        if (program == null)
            throw new NotFoundException("WorkoutProgram", request.ProgramId);

        var routine = new ProgramRoutine
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ProgramId = request.ProgramId,
            Name = request.Name,
            DayOfWeek = request.DayOfWeek
        };

        _context.ProgramRoutines.Add(routine);
        await _context.SaveChangesAsync(cancellationToken);

        return routine.Id;
    }
}
