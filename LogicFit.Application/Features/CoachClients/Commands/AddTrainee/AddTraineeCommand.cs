using MediatR;

namespace LogicFit.Application.Features.CoachClients.Commands.AddTrainee;

public class AddTraineeCommand : IRequest<Guid>
{
    public string ClientName { get; set; } = string.Empty;
    public string ClientPhone { get; set; } = string.Empty;
    public string? ClientEmail { get; set; }
    public int? Gender { get; set; }
    public DateTime? BirthDate { get; set; }
    public double? HeightCm { get; set; }
    public string? ActivityLevel { get; set; }
    public string? MedicalHistory { get; set; }
    public string? Notes { get; set; }
}
