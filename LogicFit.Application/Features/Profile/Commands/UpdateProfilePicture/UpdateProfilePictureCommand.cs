using MediatR;

namespace LogicFit.Application.Features.Profile.Commands.UpdateProfilePicture;

public class UpdateProfilePictureCommand : IRequest<string>
{
    public string ProfilePictureUrl { get; set; } = string.Empty;
}
