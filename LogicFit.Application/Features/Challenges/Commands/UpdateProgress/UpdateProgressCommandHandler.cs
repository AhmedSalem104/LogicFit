using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Challenges.Commands.UpdateProgress;

public class UpdateProgressCommandHandler : IRequestHandler<UpdateProgressCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;

    public UpdateProgressCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeService dateTimeService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
    }

    public async Task<bool> Handle(UpdateProgressCommand request, CancellationToken cancellationToken)
    {
        var clientId = Guid.Parse(_currentUserService.UserId!);

        var clientChallenge = await _context.ClientChallenges
            .Include(cc => cc.Challenge)
            .FirstOrDefaultAsync(cc => cc.ChallengeId == request.ChallengeId && cc.ClientId == clientId, cancellationToken);

        if (clientChallenge == null)
            throw new NotFoundException("ClientChallenge", request.ChallengeId);

        var challenge = clientChallenge.Challenge;
        var now = _dateTimeService.UtcNow;

        // Progress can only be logged while the challenge is running.
        if (challenge.Status != ChallengeStatus.Active)
            throw new DomainException("This challenge is not active");
        if (now < challenge.StartDate || now > challenge.EndDate)
            throw new DomainException("This challenge is not currently running");

        // Add to the running total by default; never let progress go negative.
        var newProgress = request.Increment ? clientChallenge.CurrentProgress + request.Progress : request.Progress;
        clientChallenge.CurrentProgress = Math.Max(0, newProgress);

        if (challenge.TargetValue.HasValue &&
            clientChallenge.CurrentProgress >= challenge.TargetValue.Value &&
            !clientChallenge.IsCompleted)
        {
            clientChallenge.IsCompleted = true;
            clientChallenge.CompletedAt = now;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
