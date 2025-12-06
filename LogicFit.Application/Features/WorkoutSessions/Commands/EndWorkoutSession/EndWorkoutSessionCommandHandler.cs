using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.WorkoutSessions.Commands.EndWorkoutSession;

public class EndWorkoutSessionCommandHandler : IRequestHandler<EndWorkoutSessionCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly IDateTimeService _dateTimeService;

    public EndWorkoutSessionCommandHandler(
        IApplicationDbContext context,
        ITenantService tenantService,
        IDateTimeService dateTimeService)
    {
        _context = context;
        _tenantService = tenantService;
        _dateTimeService = dateTimeService;
    }

    public async Task<bool> Handle(EndWorkoutSessionCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var session = await _context.WorkoutSessions
            .Include(s => s.Sets)
            .FirstOrDefaultAsync(s => s.Id == request.SessionId && s.TenantId == tenantId, cancellationToken);

        if (session == null)
            throw new NotFoundException("WorkoutSession", request.SessionId);

        session.EndedAt = _dateTimeService.Now;
        session.Notes = request.Notes;
        session.TotalVolumLifted = session.Sets.Sum(s => s.VolumeLoad);

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
