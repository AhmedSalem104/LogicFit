using MediatR;

namespace LogicFit.Application.Features.GymProfile.Commands.UpdateGymProfile;

public class UpdateGymProfileCommand : IRequest<bool>
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Address { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? LogoUrl { get; set; }
    public string? CoverImageUrl { get; set; }
    public List<string>? GalleryImages { get; set; }
    public string? PrimaryColor { get; set; }
    public string? SecondaryColor { get; set; }
}
