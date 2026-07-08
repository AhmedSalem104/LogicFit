using LogicFit.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Auth.Commands.ForgetPassword;

public class ForgetPasswordCommandHandler : IRequestHandler<ForgetPasswordCommand, ForgetPasswordResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public ForgetPasswordCommandHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<ForgetPasswordResponse> Handle(ForgetPasswordCommand request, CancellationToken cancellationToken)
    {
        var tenantId = await Common.TenantResolver.ResolveAsync(request.TenantId, request.Subdomain, _tenantService);

        var user = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.TenantId == tenantId &&
                                      u.PhoneNumber == request.PhoneNumber &&
                                      u.IsActive && !u.IsDeleted,
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
