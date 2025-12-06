using LogicFit.Application.Features.Muscles.DTOs;
using MediatR;

namespace LogicFit.Application.Features.Muscles.Queries.GetMuscles;

public class GetMusclesQuery : IRequest<List<MuscleDto>>
{
    public string? BodyPart { get; set; }
}
