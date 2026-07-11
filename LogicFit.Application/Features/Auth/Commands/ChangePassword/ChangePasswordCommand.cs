using MediatR;

namespace LogicFit.Application.Features.Auth.Commands.ChangePassword;

/// <summary>Authenticated self-service password change. The user is resolved from the JWT.</summary>
public class ChangePasswordCommand : IRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}
