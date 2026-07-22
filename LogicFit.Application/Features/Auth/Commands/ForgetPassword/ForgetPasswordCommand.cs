using MediatR;

namespace LogicFit.Application.Features.Auth.Commands.ForgetPassword;

public class ForgetPasswordCommand : IRequest<ForgetPasswordResponse>
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Subdomain { get; set; }
    public Guid TenantId { get; set; }
}

public class ForgetPasswordResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    /// <summary>Only populated in Development when explicitly enabled; never returned in Production.</summary>
    public string? ResetToken { get; set; }
}
