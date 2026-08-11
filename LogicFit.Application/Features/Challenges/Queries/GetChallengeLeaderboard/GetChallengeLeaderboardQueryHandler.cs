using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Challenges.DTOs;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Challenges.Queries.GetChallengeLeaderboard;

public class GetChallengeLeaderboardQueryHandler
    : IRequestHandler<GetChallengeLeaderboardQuery, List<ChallengeLeaderboardEntryDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public GetChallengeLeaderboardQueryHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<List<ChallengeLeaderboardEntryDto>> Handle(
        GetChallengeLeaderboardQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var challenge = await _context.Challenges
            .FirstOrDefaultAsync(c => c.Id == request.ChallengeId
                && c.TenantId == tenantId
                && !c.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Challenge", request.ChallengeId);

        var target = challenge.TargetValue;

        var participants = await _context.ClientChallenges
            .Where(cc => cc.ChallengeId == request.ChallengeId && cc.TenantId == tenantId)
            // Finishers first, then highest progress, then whoever completed earliest.
            .OrderByDescending(cc => cc.IsCompleted)
            .ThenByDescending(cc => cc.CurrentProgress)
            .ThenBy(cc => cc.CompletedAt)
            .Select(cc => new
            {
                cc.ClientId,
                ClientName = cc.Client.Profile != null ? cc.Client.Profile.FullName ?? cc.Client.Email : cc.Client.Email,
                cc.CurrentProgress,
                cc.IsCompleted,
                cc.CompletedAt
            })
            .ToListAsync(cancellationToken);

        return participants
            .Select((p, index) => new ChallengeLeaderboardEntryDto
            {
                Rank = index + 1,
                ClientId = p.ClientId,
                ClientName = p.ClientName,
                CurrentProgress = p.CurrentProgress,
                ProgressPercentage = target.HasValue && target.Value > 0
                    ? Math.Min(100, Math.Round(p.CurrentProgress / target.Value * 100, 1))
                    : 0,
                IsCompleted = p.IsCompleted,
                CompletedAt = p.CompletedAt
            })
            .ToList();
    }
}
