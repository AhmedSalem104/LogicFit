using MediatR;

namespace LogicFit.Application.Features.GroupClasses.Commands.CreateGroupClass;

public class CreateGroupClassCommand : IRequest<Guid>
{
    public Guid? BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public int DurationMinutes { get; set; } = 60;
    public int Capacity { get; set; } = 20;
    public string? Color { get; set; }
    public string? ImageUrl { get; set; }
    public decimal? Price { get; set; }
    public bool IsActive { get; set; } = true;
}
