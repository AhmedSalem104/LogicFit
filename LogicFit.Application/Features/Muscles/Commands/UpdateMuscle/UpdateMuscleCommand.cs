using LogicFit.Application.Features.Muscles.DTOs;
using MediatR;

namespace LogicFit.Application.Features.Muscles.Commands.UpdateMuscle;

public class UpdateMuscleCommand : IRequest<MuscleDto>
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public string? BodyPart { get; set; }
    public string? Description { get; set; }
    public string? DescriptionAr { get; set; }
    public string? Icon { get; set; }
}
