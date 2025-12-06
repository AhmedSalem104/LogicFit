using LogicFit.Application.Features.CoachClients.DTOs;
using MediatR;

namespace LogicFit.Application.Features.CoachClients.Queries.GetCoachClients;

public class GetCoachClientsQuery : IRequest<List<CoachClientDto>>
{
    public Guid? CoachId { get; set; }  // If null, uses current user (for coaches)
    public bool? IsActive { get; set; } = true;  // Default: only active assignments
}
