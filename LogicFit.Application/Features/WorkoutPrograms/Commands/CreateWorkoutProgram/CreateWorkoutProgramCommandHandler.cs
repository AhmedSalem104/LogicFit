using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using MediatR;

namespace LogicFit.Application.Features.WorkoutPrograms.Commands.CreateWorkoutProgram;

public class CreateWorkoutProgramCommandHandler : IRequestHandler<CreateWorkoutProgramCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;

    public CreateWorkoutProgramCommandHandler(
        IApplicationDbContext context,
        ITenantService tenantService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(CreateWorkoutProgramCommand request, CancellationToken cancellationToken)
    {
        var program = new WorkoutProgram
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantService.GetCurrentTenantId(),
            CoachId = Guid.Parse(_currentUserService.UserId!),
            ClientId = request.ClientId,
            Name = request.Name,
            StartDate = request.StartDate,
            EndDate = request.EndDate
        };

        _context.WorkoutPrograms.Add(program);
        await _context.SaveChangesAsync(cancellationToken);

        return program.Id;
    }
}
