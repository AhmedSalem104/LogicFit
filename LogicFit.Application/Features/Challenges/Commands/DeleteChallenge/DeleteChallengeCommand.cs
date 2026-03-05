using MediatR;

namespace LogicFit.Application.Features.Challenges.Commands.DeleteChallenge;

public class DeleteChallengeCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}
