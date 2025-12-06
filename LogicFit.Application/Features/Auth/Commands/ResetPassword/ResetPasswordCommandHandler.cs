using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Auth.Commands.ResetPassword;

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, ResetPasswordResponse>
{
    private readonly IApplicationDbContext _context;

    public ResetPasswordCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ResetPasswordResponse> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.TenantId == request.TenantId &&
                                      u.PhoneNumber == request.PhoneNumber &&
                                      u.IsActive,
                                 cancellationToken);

        if (user == null)
        {
            throw new NotFoundException("User", request.PhoneNumber);
        }

        // Validate reset token
        if (user.PasswordResetToken != request.ResetToken)
        {
            throw new ValidationException("Invalid reset token.");
        }

        // Check token expiry
        if (user.PasswordResetTokenExpiry == null || user.PasswordResetTokenExpiry < DateTime.UtcNow)
        {
            throw new ValidationException("Reset token has expired. Please request a new one.");
        }

        // Update password
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;

        await _context.SaveChangesAsync(cancellationToken);

        return new ResetPasswordResponse
        {
            Success = true,
            Message = "Password has been reset successfully."
        };
    }
}
