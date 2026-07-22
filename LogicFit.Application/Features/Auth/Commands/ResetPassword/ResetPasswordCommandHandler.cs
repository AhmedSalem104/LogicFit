using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Auth.Commands.ResetPassword;

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, ResetPasswordResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly IPasswordResetTokenService _resetTokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;

    public ResetPasswordCommandHandler(
        IApplicationDbContext context,
        ITenantService tenantService,
        IPasswordResetTokenService resetTokenService,
        IRefreshTokenService refreshTokenService,
        ICurrentUserService currentUserService,
        IDateTimeService dateTimeService)
    {
        _context = context;
        _tenantService = tenantService;
        _resetTokenService = resetTokenService;
        _refreshTokenService = refreshTokenService;
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
    }

    public async Task<ResetPasswordResponse> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
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
            throw new ValidationException("Invalid or expired reset token.");
        }

        if (string.IsNullOrWhiteSpace(user.PasswordResetToken)
            || !_resetTokenService.VerifyToken(request.ResetToken, user.PasswordResetToken))
        {
            throw new ValidationException("Invalid or expired reset token.");
        }

        if (user.PasswordResetTokenExpiry == null || user.PasswordResetTokenExpiry < _dateTimeService.UtcNow)
        {
            throw new ValidationException("Invalid or expired reset token.");
        }

        // Update password
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;
        user.PermissionsVersion++;

        await _refreshTokenService.RevokeAllAsync(user.Id, _currentUserService.IpAddress, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return new ResetPasswordResponse
        {
            Success = true,
            Message = "Password has been reset successfully."
        };
    }
}
