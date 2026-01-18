using LogicFit.Application.Features.CoachClients.DTOs;
using MediatR;

namespace LogicFit.Application.Features.CoachClients.Queries.GetCoachClientById;

public class GetCoachClientByIdQuery : IRequest<CoachClientDto?>
{
    public Guid Id { get; set; }
}
