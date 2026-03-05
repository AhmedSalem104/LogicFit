using LogicFit.Application.Features.Challenges.DTOs;
using MediatR;

namespace LogicFit.Application.Features.Challenges.Queries.GetMyChallenges;

public class GetMyChallengesQuery : IRequest<List<ClientChallengeDto>>
{
}
