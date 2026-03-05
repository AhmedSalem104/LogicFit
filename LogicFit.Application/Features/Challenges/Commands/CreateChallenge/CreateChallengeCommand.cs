using MediatR;

namespace LogicFit.Application.Features.Challenges.Commands.CreateChallenge;

public class CreateChallengeCommand : IRequest<Guid>
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? TargetMetric { get; set; }
    public double? TargetValue { get; set; }
    public List<Guid>? ClientIds { get; set; }
}
