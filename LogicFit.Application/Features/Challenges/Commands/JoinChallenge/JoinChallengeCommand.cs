using MediatR;

namespace LogicFit.Application.Features.Challenges.Commands.JoinChallenge;

public class JoinChallengeCommand : IRequest<Guid>
{
    public Guid ChallengeId { get; set; }
}
