using LogicFit.Application.Features.Clients.DTOs;
using MediatR;

namespace LogicFit.Application.Features.Clients.Queries.GetClientTrainingOverview;

public sealed class GetClientTrainingOverviewQuery : IRequest<ClientTrainingOverviewDto?>
{
    public Guid ClientId { get; set; }
}
