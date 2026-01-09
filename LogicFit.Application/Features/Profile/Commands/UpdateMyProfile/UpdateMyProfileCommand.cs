using MediatR;

namespace LogicFit.Application.Features.Profile.Commands.UpdateMyProfile;

public class UpdateMyProfileCommand : IRequest<bool>
{
    public string? FullName { get; set; }
    public int? Gender { get; set; }
    public DateTime? BirthDate { get; set; }
    public double? HeightCm { get; set; }
    public double? WeightKg { get; set; }
    public string? ActivityLevel { get; set; }
    public string? FitnessGoal { get; set; }
    public string? MedicalHistory { get; set; }
}
