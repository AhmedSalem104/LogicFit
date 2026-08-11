using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Challenges.DTOs;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Challenges.Queries.GetMyChallenges;

public class GetMyChallengesQueryHandler : IRequestHandler<GetMyChallengesQuery, List<ClientChallengeDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;

    public GetMyChallengesQueryHandler(
        IApplicationDbContext context,
        ITenantService tenantService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
    }

    public async Task<List<ClientChallengeDto>> Handle(GetMyChallengesQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        if (!Guid.TryParse(_currentUserService.UserId, out var clientId))
            throw new UnauthorizedException("An authenticated workspace user is required.");

        var clientChallenges = await _context.ClientChallenges
            .Where(cc => cc.TenantId == tenantId
                && cc.ClientId == clientId
                && cc.Challenge.TenantId == tenantId
                && !cc.Challenge.IsDeleted)
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
