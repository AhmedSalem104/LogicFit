using LogicFit.Application.Features.Users.DTOs;
using MediatR;

namespace LogicFit.Application.Features.Profile.Queries.GetMyProfile;

public class GetMyProfileQuery : IRequest<UserDto?>
{
}
