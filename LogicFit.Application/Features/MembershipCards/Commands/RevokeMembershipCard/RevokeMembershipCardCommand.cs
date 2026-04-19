using MediatR;

namespace LogicFit.Application.Features.MembershipCards.Commands.RevokeMembershipCard;

public class RevokeMembershipCardCommand : IRequest
{
    public Guid Id { get; set; }
    public string? Reason { get; set; }
}
