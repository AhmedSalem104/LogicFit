using MediatR;

namespace LogicFit.Application.Features.Clients.Commands.CreateClient;

public class CreateClientCommand : IRequest<Guid>
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Password { get; set; }  // Optional - auto-generated if not provided
    public string? FullName { get; set; }
    public int? Gender { get; set; }
    public DateTime? BirthDate { get; set; }
    public double? HeightCm { get; set; }
    public string? ActivityLevel { get; set; }
    public string? MedicalHistory { get; set; }
    public Guid? CoachId { get; set; }  // Optional - auto-assign to coach if provided
}
