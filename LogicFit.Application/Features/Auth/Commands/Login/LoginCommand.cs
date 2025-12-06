using LogicFit.Application.Features.Auth.DTOs;
using MediatR;

namespace LogicFit.Application.Features.Auth.Commands.Login;

public class LoginCommand : IRequest<AuthResponseDto>
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
}
