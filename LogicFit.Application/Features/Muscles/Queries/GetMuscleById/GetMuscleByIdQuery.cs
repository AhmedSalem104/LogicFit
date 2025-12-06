using LogicFit.Application.Features.Muscles.DTOs;
using MediatR;

namespace LogicFit.Application.Features.Muscles.Queries.GetMuscleById;

public class GetMuscleByIdQuery : IRequest<MuscleDto?>
{
    public int Id { get; set; }
}
