using MediatR;

namespace LogicFit.Application.Features.CoachClients.Commands.AddTrainee;

public class AddTraineeCommand : IRequest<AddTraineeResult>
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
    public string? TemporaryPassword { get; set; }
}

public sealed class AddTraineeResult
{
    public Guid ClientId { get; init; }
    public string ClientPhone { get; init; } = string.Empty;
    public string TemporaryPassword { get; init; } = string.Empty;
    public bool MustChangePassword { get; init; } = true;
}
