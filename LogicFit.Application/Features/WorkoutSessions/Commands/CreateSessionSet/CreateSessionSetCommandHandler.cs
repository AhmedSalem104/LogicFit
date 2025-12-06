using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.WorkoutSessions.Commands.CreateSessionSet;

public class CreateSessionSetCommandHandler : IRequestHandler<CreateSessionSetCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;

    public CreateSessionSetCommandHandler(
        IApplicationDbContext context,
        ITenantService tenantService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(CreateSessionSetCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var userId = Guid.Parse(_currentUserService.UserId!);

        var session = await _context.WorkoutSessions
            .FirstOrDefaultAsync(s => s.Id == request.SessionId && s.TenantId == tenantId, cancellationToken);

        if (session == null)
            throw new NotFoundException("WorkoutSession", request.SessionId);

        var volumeLoad = request.WeightKg * request.Reps;

        // Check for PR
        var previousBestVolume = await _context.SessionSets
            .Where(s => s.ExerciseId == request.ExerciseId &&
                       s.Session.ClientId == userId &&
                       s.Session.TenantId == tenantId)
            .MaxAsync(s => (double?)s.VolumeLoad, cancellationToken) ?? 0;

        var isPr = volumeLoad > previousBestVolume;

        var set = new SessionSet
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SessionId = request.SessionId,
            ExerciseId = request.ExerciseId,
            SetNumber = request.SetNumber,
            WeightKg = request.WeightKg,
            Reps = request.Reps,
            Rpe = request.Rpe,
            VolumeLoad = volumeLoad,
            IsPr = isPr
        };

        _context.SessionSets.Add(set);
        await _context.SaveChangesAsync(cancellationToken);

        return set.Id;
    }
}
