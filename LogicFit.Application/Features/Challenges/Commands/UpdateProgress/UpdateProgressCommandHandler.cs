using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Challenges.Commands.UpdateProgress;

public class UpdateProgressCommandHandler : IRequestHandler<UpdateProgressCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public UpdateProgressCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<bool> Handle(UpdateProgressCommand request, CancellationToken cancellationToken)
    {
        var clientId = Guid.Parse(_currentUserService.UserId!);

        var clientChallenge = await _context.ClientChallenges
            .Include(cc => cc.Challenge)
            .FirstOrDefaultAsync(cc => cc.ChallengeId == request.ChallengeId && cc.ClientId == clientId, cancellationToken);

        if (clientChallenge == null)
            throw new NotFoundException("ClientChallenge", request.ChallengeId);

        clientChallenge.CurrentProgress = request.Progress;

        if (clientChallenge.Challenge.TargetValue.HasValue &&
            request.Progress >= clientChallenge.Challenge.TargetValue.Value &&
            !clientChallenge.IsCompleted)
        {
            clientChallenge.IsCompleted = true;
            clientChallenge.CompletedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
