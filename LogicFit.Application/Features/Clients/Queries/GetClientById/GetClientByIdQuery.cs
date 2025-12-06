using LogicFit.Application.Features.Clients.DTOs;
using MediatR;

namespace LogicFit.Application.Features.Clients.Queries.GetClientById;

public class GetClientByIdQuery : IRequest<ClientDto?>
{
    public Guid Id { get; set; }
}
