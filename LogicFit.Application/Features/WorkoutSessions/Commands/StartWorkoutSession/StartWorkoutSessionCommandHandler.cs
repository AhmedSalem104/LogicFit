using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using MediatR;

namespace LogicFit.Application.Features.WorkoutSessions.Commands.StartWorkoutSession;

public class StartWorkoutSessionCommandHandler : IRequestHandler<StartWorkoutSessionCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;

    public StartWorkoutSessionCommandHandler(
        IApplicationDbContext context,
        ITenantService tenantService,
        ICurrentUserService currentUserService,
        IDateTimeService dateTimeService)
    {
        _context = context;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
    }

    public async Task<Guid> Handle(StartWorkoutSessionCommand request, CancellationToken cancellationToken)
    {
        var session = new WorkoutSession
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantService.GetCurrentTenantId(),
            ClientId = Guid.Parse(_currentUserService.UserId!),
            RoutineId = request.RoutineId,
            StartedAt = _dateTimeService.Now,
            TotalVolumLifted = 0
        };

        _context.WorkoutSessions.Add(session);
        await _context.SaveChangesAsync(cancellationToken);

        return session.Id;
    }
}
