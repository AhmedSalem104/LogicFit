using MediatR;

namespace LogicFit.Application.Features.GroupClasses.Commands.UpdateGroupClass;

public class UpdateGroupClassCommand : IRequest
{
    public Guid Id { get; set; }
    public Guid? BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public int DurationMinutes { get; set; }
    public int Capacity { get; set; }
    public string? Color { get; set; }
    public string? ImageUrl { get; set; }
    public decimal? Price { get; set; }
    public bool IsActive { get; set; }
}
