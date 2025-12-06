using MediatR;

namespace LogicFit.Application.Features.Auth.Commands.ForgetPassword;

public class ForgetPasswordCommand : IRequest<ForgetPasswordResponse>
{
    public string PhoneNumber { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
}

public class ForgetPasswordResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ResetToken { get; set; } // In production, this would be sent via SMS/Email
}
