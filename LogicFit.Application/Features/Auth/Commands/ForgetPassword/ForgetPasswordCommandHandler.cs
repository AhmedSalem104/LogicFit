using LogicFit.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Auth.Commands.ForgetPassword;

public class ForgetPasswordCommandHandler : IRequestHandler<ForgetPasswordCommand, ForgetPasswordResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly IPasswordResetTokenService _resetTokenService;
    private readonly IDateTimeService _dateTimeService;

    public ForgetPasswordCommandHandler(
        IApplicationDbContext context,
        ITenantService tenantService,
        IPasswordResetTokenService resetTokenService,
        IDateTimeService dateTimeService)
    {
        _context = context;
        _tenantService = tenantService;
        _resetTokenService = resetTokenService;
        _dateTimeService = dateTimeService;
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
        var resetToken = _resetTokenService.GenerateToken();

        user.PasswordResetToken = _resetTokenService.HashToken(resetToken);
        user.PasswordResetTokenExpiry = _dateTimeService.UtcNow.AddMinutes(15);

        await _context.SaveChangesAsync(cancellationToken);

        // In production, send SMS here
        // For development, return the token directly
        return new ForgetPasswordResponse
        {
            Success = true,
            Message = "Reset code has been sent to your phone number.",
            ResetToken = _resetTokenService.CanExposeToken ? resetToken : null
        };
    }
}
