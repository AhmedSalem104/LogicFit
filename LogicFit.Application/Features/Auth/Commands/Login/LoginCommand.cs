using LogicFit.Application.Features.Auth.DTOs;
using MediatR;

namespace LogicFit.Application.Features.Auth.Commands.Login;

public class LoginCommand : IRequest<AuthResponseDto>
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    /// <summary>The gym's subdomain (e.g. "goldgym"). Preferred — no need to know the TenantId GUID.</summary>
    public string? Subdomain { get; set; }

    /// <summary>Optional. Provide either Subdomain or TenantId; Subdomain is simpler for clients.</summary>
    public Guid TenantId { get; set; }
}
