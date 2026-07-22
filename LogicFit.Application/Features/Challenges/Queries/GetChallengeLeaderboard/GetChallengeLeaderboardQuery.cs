using LogicFit.Application.Features.Challenges.DTOs;
using MediatR;

namespace LogicFit.Application.Features.Challenges.Queries.GetChallengeLeaderboard;

/// <summary>Ranked participants of a challenge (completed first, then by progress).</summary>
public class GetChallengeLeaderboardQuery : IRequest<List<ChallengeLeaderboardEntryDto>>
{
    public Guid ChallengeId { get; set; }
}
