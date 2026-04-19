using MediatR;

namespace LogicFit.Application.Features.MembershipCards.Commands.IssueMembershipCard;

public class IssueMembershipCardCommand : IRequest<Guid>
{
    public Guid ClientId { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? CardNumber { get; set; } // optional — auto-generate if null
}
