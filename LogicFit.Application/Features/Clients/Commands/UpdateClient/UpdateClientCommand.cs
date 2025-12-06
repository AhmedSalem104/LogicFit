using MediatR;

namespace LogicFit.Application.Features.Clients.Commands.UpdateClient;

public class UpdateClientCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public string? Email { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string? FullName { get; set; }
    public int? Gender { get; set; }
    public DateTime? BirthDate { get; set; }
    public double? HeightCm { get; set; }
    public string? ActivityLevel { get; set; }
    public string? MedicalHistory { get; set; }
}
