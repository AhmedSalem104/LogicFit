using LogicFit.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Auth.Commands.ForgetPassword;

public class ForgetPasswordCommandHandler : IRequestHandler<ForgetPasswordCommand, ForgetPasswordResponse>
{
    private readonly IApplicationDbContext _context;

    public ForgetPasswordCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ForgetPasswordResponse> Handle(ForgetPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.TenantId == request.TenantId &&
                                      u.PhoneNumber == request.PhoneNumber &&
                                      u.IsActive,
                                 cancellationToken);

        if (user == null)
        {
            // Don't reveal if user exists or not for security
            return new ForgetPasswordResponse
            {
                Success = true,
                Message = "If the phone number exists, a reset code will be sent."
            };
        }

        // Generate 6-digit reset token
        var resetToken = new Random().Next(100000, 999999).ToString();

        user.PasswordResetToken = resetToken;
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddMinutes(15); // Token valid for 15 minutes

        await _context.SaveChangesAsync(cancellationToken);

        // In production, send SMS here
        // For development, return the token directly
        return new ForgetPasswordResponse
        {
            Success = true,
            Message = "Reset code has been sent to your phone number.",
            ResetToken = resetToken // Remove this in production
        };
    }
}
