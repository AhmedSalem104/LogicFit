using MediatR;

namespace LogicFit.Application.Features.Challenges.Commands.UpdateProgress;

public class UpdateProgressCommand : IRequest<bool>
{
    public Guid ChallengeId { get; set; }

    /// <summary>The progress amount. Added to the current value when <see cref="Increment"/> is true
    /// (e.g. "logged one more session"); otherwise it replaces the current value.</summary>
    public double Progress { get; set; }

    /// <summary>Add to the existing progress (default) instead of overwriting it.</summary>
    public bool Increment { get; set; } = true;
}
