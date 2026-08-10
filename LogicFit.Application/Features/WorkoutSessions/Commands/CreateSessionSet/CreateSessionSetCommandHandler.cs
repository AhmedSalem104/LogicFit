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
    private readonly ICoachPlanAccessService _accessService;

    public CreateSessionSetCommandHandler(
        IApplicationDbContext context,
        ITenantService tenantService,
        ICurrentUserService currentUserService,
        ICoachPlanAccessService accessService)
    {
        _context = context;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
        _accessService = accessService;
    }

    public async Task<Guid> Handle(CreateSessionSetCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        if (!Guid.TryParse(_currentUserService.UserId, out var userId))
            throw new ForbiddenException("An authenticated client is required.");

        var session = await _context.WorkoutSessions
            .FirstOrDefaultAsync(s => s.Id == request.SessionId && s.TenantId == tenantId, cancellationToken);

        if (session == null)
            throw new NotFoundException("WorkoutSession", request.SessionId);

        await _accessService.EnsureClientOwnsSessionAsync(request.SessionId, cancellationToken);
        if (session.EndedAt.HasValue)
            throw new ForbiddenException("A completed workout session cannot accept more sets.");

        var exerciseIsInRoutine = await _context.RoutineExercises
            .AnyAsync(e => e.RoutineId == session.RoutineId && e.ExerciseId == request.ExerciseId, cancellationToken);
        if (!exerciseIsInRoutine)
            throw new ForbiddenException("The exercise is not part of this workout routine.");

        var existingSet = await _context.SessionSets
            .FirstOrDefaultAsync(s => s.SessionId == request.SessionId
                && s.ExerciseId == request.ExerciseId
                && s.SetNumber == request.SetNumber, cancellationToken);
        if (existingSet != null)
            return existingSet.Id;

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
