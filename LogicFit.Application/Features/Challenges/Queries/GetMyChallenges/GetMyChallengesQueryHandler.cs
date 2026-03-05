using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Challenges.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Challenges.Queries.GetMyChallenges;

public class GetMyChallengesQueryHandler : IRequestHandler<GetMyChallengesQuery, List<ClientChallengeDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetMyChallengesQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<List<ClientChallengeDto>> Handle(GetMyChallengesQuery request, CancellationToken cancellationToken)
    {
        var clientId = Guid.Parse(_currentUserService.UserId!);

        var clientChallenges = await _context.ClientChallenges
            .Where(cc => cc.ClientId == clientId)
            .Select(cc => new ClientChallengeDto
            {
                Id = cc.Id,
                ChallengeId = cc.ChallengeId,
                ChallengeTitle = cc.Challenge.Title,
                ClientId = cc.ClientId,
                ClientName = cc.Client.Profile != null ? cc.Client.Profile.FullName ?? cc.Client.Email : cc.Client.Email,
                CurrentProgress = cc.CurrentProgress,
                TargetValue = cc.Challenge.TargetValue,
                IsCompleted = cc.IsCompleted,
                CompletedAt = cc.CompletedAt,
                ProgressPercentage = cc.Challenge.TargetValue.HasValue && cc.Challenge.TargetValue.Value > 0
                    ? Math.Min(100, (cc.CurrentProgress / cc.Challenge.TargetValue.Value) * 100)
                    : 0
            })
            .ToListAsync(cancellationToken);

        return clientChallenges;
    }
}
