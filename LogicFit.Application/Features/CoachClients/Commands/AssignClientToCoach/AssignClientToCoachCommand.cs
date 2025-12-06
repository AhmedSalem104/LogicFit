using MediatR;

namespace LogicFit.Application.Features.CoachClients.Commands.AssignClientToCoach;

public class AssignClientToCoachCommand : IRequest<Guid>
{
    public Guid? CoachId { get; set; }  // Optional: if null, uses current user (coach assigns to self)
    public Guid ClientId { get; set; }
    public string? Notes { get; set; }
}
