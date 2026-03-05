using MediatR;

namespace LogicFit.Application.Features.Coaches.Commands.UpdateCoach;

public class UpdateCoachCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public bool? IsActive { get; set; }
    public string? FullName { get; set; }
    public int? Gender { get; set; }
    public DateTime? BirthDate { get; set; }
}
