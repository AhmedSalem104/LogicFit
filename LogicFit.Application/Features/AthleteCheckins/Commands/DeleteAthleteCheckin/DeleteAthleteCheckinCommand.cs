using MediatR;

namespace LogicFit.Application.Features.AthleteCheckins.Commands.DeleteAthleteCheckin;

public sealed class DeleteAthleteCheckinCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public Guid ClientId { get; set; }
}
