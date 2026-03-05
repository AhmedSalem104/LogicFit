using LogicFit.Application.Features.Coaches.DTOs;
using MediatR;

namespace LogicFit.Application.Features.Coaches.Queries.GetCoachById;

public class GetCoachByIdQuery : IRequest<CoachDto?>
{
    public Guid Id { get; set; }
}
