using LogicFit.Application.Features.Challenges.DTOs;
using LogicFit.Domain.Enums;
using MediatR;

namespace LogicFit.Application.Features.Challenges.Queries.GetChallenges;

public class GetChallengesQuery : IRequest<List<ChallengeDto>>
{
    public ChallengeStatus? Status { get; set; }
}
