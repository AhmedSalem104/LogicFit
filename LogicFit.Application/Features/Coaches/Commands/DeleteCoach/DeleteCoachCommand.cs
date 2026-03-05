using MediatR;

namespace LogicFit.Application.Features.Coaches.Commands.DeleteCoach;

public class DeleteCoachCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}
