using MediatR;

namespace LogicFit.Application.Features.Subscriptions.Commands.EndFreezeEarly;

public class EndFreezeEarlyCommand : IRequest<bool>
{
    public Guid FreezeId { get; set; }
}
