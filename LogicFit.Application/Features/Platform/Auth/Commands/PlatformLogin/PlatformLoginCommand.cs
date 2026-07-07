using LogicFit.Application.Features.Auth.DTOs;
using MediatR;

namespace LogicFit.Application.Features.Platform.Auth.Commands.PlatformLogin;

/// <summary>Login for platform users (PlatformOwner/PlatformAdmin) by email + password, no tenant.</summary>
public class PlatformLoginCommand : IRequest<AuthResponseDto>
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
