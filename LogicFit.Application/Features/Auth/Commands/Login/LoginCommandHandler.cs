using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Auth.DTOs;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Auth.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponseDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly IDateTimeService _dateTimeService;
    private readonly IRbacService _rbacService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ICurrentUserService _currentUserService;

    public LoginCommandHandler(
        IApplicationDbContext context,
        IJwtService jwtService,
        IDateTimeService dateTimeService,
        IRbacService rbacService,
        IRefreshTokenService refreshTokenService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _jwtService = jwtService;
        _dateTimeService = dateTimeService;
        _rbacService = rbacService;
        _refreshTokenService = refreshTokenService;
        _currentUserService = currentUserService;
    }

    public async Task<AuthResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // Find user by phone number (include profile for FullName)
        var user = await _context.Users
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.TenantId == request.TenantId && u.PhoneNumber == request.PhoneNumber,
                cancellationToken);

        if (user == null)
        {
            throw new UnauthorizedException("Invalid credentials");
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedException("Account is deactivated");
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedException("Invalid credentials");
        }

        // Resolve roles + permissions from RBAC tables
        var auth = await _rbacService.GetUserAuthorizationAsync(user.Id, cancellationToken);

        var accessToken = _jwtService.GenerateAccessToken(
            user.Id, user.Email, user.TenantId, auth.Roles, auth.Permissions, user.PermissionsVersion);

        var refreshToken = _refreshTokenService.Issue(
            user, _currentUserService.IpAddress, Common.Services.RefreshTokenService.SurfaceTenant);
        await _context.SaveChangesAsync(cancellationToken);

        return new AuthResponseDto
        {
            UserId = user.Id,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            FullName = user.Profile?.FullName,
            Role = user.Role.ToString(),
            Roles = auth.Roles,
            Permissions = auth.Permissions,
            TenantId = user.TenantId,
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            ExpiresAt = _dateTimeService.UtcNow.AddMinutes(60)
        };
    }
}
