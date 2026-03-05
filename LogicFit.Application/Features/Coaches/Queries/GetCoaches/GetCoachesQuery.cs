using LogicFit.Application.Features.Coaches.DTOs;
using MediatR;

namespace LogicFit.Application.Features.Coaches.Queries.GetCoaches;

public class GetCoachesQuery : IRequest<List<CoachDto>>
{
    public string? SearchTerm { get; set; }
    public bool? IsActive { get; set; }
}
