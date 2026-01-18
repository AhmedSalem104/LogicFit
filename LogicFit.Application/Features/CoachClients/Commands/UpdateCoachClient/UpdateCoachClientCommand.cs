using MediatR;

namespace LogicFit.Application.Features.CoachClients.Commands.UpdateCoachClient;

public class UpdateCoachClientCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public Guid? NewCoachId { get; set; }
    public bool? IsActive { get; set; }
}
