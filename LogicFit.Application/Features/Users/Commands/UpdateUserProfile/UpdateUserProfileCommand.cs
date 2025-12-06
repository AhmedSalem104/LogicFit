using MediatR;

namespace LogicFit.Application.Features.Users.Commands.UpdateUserProfile;

public class UpdateUserProfileCommand : IRequest<bool>
{
    public Guid UserId { get; set; }
    public string? FullName { get; set; }
    public int? Gender { get; set; }
    public DateTime? BirthDate { get; set; }
    public double? HeightCm { get; set; }
    public string? ActivityLevel { get; set; }
    public string? MedicalHistory { get; set; }
}
