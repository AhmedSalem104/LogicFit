using LogicFit.Domain.Enums;
using MediatR;

namespace LogicFit.Application.Features.Users.Commands.CreateStaffUser;

/// <summary>
/// Creates a back-office staff user (Manager / Receptionist / Accountant / Trainer) with the matching
/// RBAC role. Owners are created via platform onboarding; Coaches and Clients have dedicated endpoints.
/// </summary>
public class CreateStaffUserCommand : IRequest<Guid>
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Password { get; set; }   // auto-generated if omitted
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
}
