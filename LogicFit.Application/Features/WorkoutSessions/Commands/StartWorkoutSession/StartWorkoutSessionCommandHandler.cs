using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using MediatR;

namespace LogicFit.Application.Features.WorkoutSessions.Commands.StartWorkoutSession;

public class StartWorkoutSessionCommandHandler : IRequestHandler<StartWorkoutSessionCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;
    private readonly ICoachPlanAccessService _accessService;

    public StartWorkoutSessionCommandHandler(
        IApplicationDbContext context,
        ITenantService tenantService,
        ICurrentUserService currentUserService,
        IDateTimeService dateTimeService,
        ICoachPlanAccessService accessService)
    {
        _context = context;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
        _accessService = accessService;
    }

    public async Task<Guid> Handle(StartWorkoutSessionCommand request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(_currentUserService.UserId, out var clientId))
            throw new ForbiddenException("An authenticated client is required.");

        await _accessService.EnsureClientOwnsRoutineAsync(request.RoutineId, cancellationToken);
        var tenantId = _tenantService.GetCurrentTenantId();
        var activeSession = await _context.WorkoutSessions
            .Where(s => s.TenantId == tenantId
                && s.ClientId == clientId
                && s.RoutineId == request.RoutineId
                && s.EndedAt == null)
            .OrderByDescending(s => s.StartedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (activeSession != null)
            return activeSession.Id;

        var session = new WorkoutSession
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ClientId = clientId,
            RoutineId = request.RoutineId,
            StartedAt = _dateTimeService.Now,
            TotalVolumLifted = 0
        };

        _context.WorkoutSessions.Add(session);
        await _context.SaveChangesAsync(cancellationToken);

        return session.Id;
    }
}
