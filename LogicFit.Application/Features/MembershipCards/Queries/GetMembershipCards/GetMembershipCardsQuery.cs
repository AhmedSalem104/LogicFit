using LogicFit.Application.Features.MembershipCards.DTOs;
using MediatR;

namespace LogicFit.Application.Features.MembershipCards.Queries.GetMembershipCards;

public class GetMembershipCardsQuery : IRequest<List<MembershipCardDto>>
{
    public Guid? ClientId { get; set; }
    public bool? IsActive { get; set; }
}
