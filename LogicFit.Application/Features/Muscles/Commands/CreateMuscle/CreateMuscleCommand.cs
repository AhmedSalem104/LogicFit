using LogicFit.Application.Features.Muscles.DTOs;
using MediatR;

namespace LogicFit.Application.Features.Muscles.Commands.CreateMuscle;

public class CreateMuscleCommand : IRequest<MuscleDto>
{
    public string Name { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public string? BodyPart { get; set; }
    public string? Description { get; set; }
    public string? DescriptionAr { get; set; }
    public string? Icon { get; set; }
}
