using MediatR;

namespace LogicFit.Application.Features.Challenges.Commands.UpdateProgress;

public class UpdateProgressCommand : IRequest<bool>
{
    public Guid ChallengeId { get; set; }
    public double Progress { get; set; }
}
