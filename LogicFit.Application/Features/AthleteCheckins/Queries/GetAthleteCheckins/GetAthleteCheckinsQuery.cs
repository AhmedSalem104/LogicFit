using LogicFit.Application.Features.AthleteCheckins.DTOs;
using MediatR;

namespace LogicFit.Application.Features.AthleteCheckins.Queries.GetAthleteCheckins;

public sealed class GetAthleteCheckinsQuery : IRequest<List<AthleteCheckinDto>>
{
    public Guid ClientId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
