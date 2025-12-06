namespace LogicFit.Application.Features.Muscles.DTOs;

public class MuscleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? BodyPart { get; set; }
}
