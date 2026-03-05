using LogicFit.Domain.Enums;
using MediatR;

namespace LogicFit.Application.Features.Challenges.Commands.UpdateChallenge;

public class UpdateChallengeCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public DateTime? EndDate { get; set; }
    public ChallengeStatus? Status { get; set; }
}
