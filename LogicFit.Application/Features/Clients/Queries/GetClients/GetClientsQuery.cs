using LogicFit.Application.Features.Clients.DTOs;
using MediatR;

namespace LogicFit.Application.Features.Clients.Queries.GetClients;

public class GetClientsQuery : IRequest<List<ClientDto>>
{
    public string? SearchTerm { get; set; }
    public bool? IsActive { get; set; }
}
