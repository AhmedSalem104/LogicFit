using LogicFit.Application.Features.Auth.DTOs;
using LogicFit.Domain.Enums;
using MediatR;

namespace LogicFit.Application.Features.Auth.Commands.Register;

public class RegisterCommand : IRequest<AuthResponseDto>
{
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public UserRole Role { get; set; } = UserRole.Client;
    public string FullName { get; set; } = string.Empty;
}
