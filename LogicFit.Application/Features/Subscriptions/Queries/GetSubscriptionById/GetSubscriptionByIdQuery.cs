using LogicFit.Application.Features.Subscriptions.DTOs;
using MediatR;

namespace LogicFit.Application.Features.Subscriptions.Queries.GetSubscriptionById;

public class GetSubscriptionByIdQuery : IRequest<ClientSubscriptionDetailDto?>
{
    public Guid Id { get; set; }
}
