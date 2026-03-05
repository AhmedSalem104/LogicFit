using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Challenges.Commands.JoinChallenge;

public class JoinChallengeCommandHandler : IRequestHandler<JoinChallengeCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;

    public JoinChallengeCommandHandler(
        IApplicationDbContext context,
        ITenantService tenantService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(JoinChallengeCommand request, CancellationToken cancellationToken)
    {
        var challenge = await _context.Challenges
            .FirstOrDefaultAsync(c => c.Id == request.ChallengeId, cancellationToken);

        if (challenge == null)
            throw new NotFoundException("Challenge", request.ChallengeId);

        var clientId = Guid.Parse(_currentUserService.UserId!);

        var alreadyJoined = await _context.ClientChallenges
            .AnyAsync(cc => cc.ChallengeId == request.ChallengeId && cc.ClientId == clientId, cancellationToken);

        if (alreadyJoined)
            throw new ConflictException("You have already joined this challenge.");

        var clientChallenge = new ClientChallenge
        {
            TenantId = _tenantService.GetCurrentTenantId(),
            ChallengeId = request.ChallengeId,
            ClientId = clientId,
            CurrentProgress = 0,
            IsCompleted = false
        };

        _context.ClientChallenges.Add(clientChallenge);
        await _context.SaveChangesAsync(cancellationToken);

        return clientChallenge.Id;
    }
}
