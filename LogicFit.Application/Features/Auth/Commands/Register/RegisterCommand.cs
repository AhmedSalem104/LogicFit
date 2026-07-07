using LogicFit.Application.Features.Auth.DTOs;
using MediatR;

namespace LogicFit.Application.Features.Auth.Commands.Register;

// Public self-registration ALWAYS creates a Client. Staff/Owner accounts are created through
// authenticated, permission-guarded flows — never by accepting a role from the request body.
public class RegisterCommand : IRequest<AuthResponseDto>
{
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public string FullName { get; set; } = string.Empty;
}
