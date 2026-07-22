using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Auth.DTOs;
using LogicFit.Domain.Authorization;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Platform.Auth.Commands.PlatformLogin;

public class PlatformLoginCommandHandler : IRequestHandler<PlatformLoginCommand, AuthResponseDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly IDateTimeService _dateTimeService;
    private readonly IRbacService _rbacService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ICurrentUserService _currentUserService;

    public PlatformLoginCommandHandler(
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

    public async Task<AuthResponseDto> Handle(PlatformLoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .IgnoreQueryFilters()
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u =>
                    u.TenantId == PlatformConstants.PlatformTenantId &&
                    u.Email == request.Email &&
                    !u.IsDeleted,
                cancellationToken);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedException("Invalid credentials");
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedException("Account is deactivated");
        }

        var auth = await _rbacService.GetUserAuthorizationAsync(user.Id, cancellationToken);

        // Platform tokens carry NO TenantId claim (so CurrentTenantId stays null => cross-tenant read).
        var accessToken = _jwtService.GenerateAccessToken(
            user.Id, user.Email, tenantId: null, auth.Roles, auth.Permissions, user.PermissionsVersion);

        var refreshToken = _refreshTokenService.Issue(
            user, _currentUserService.IpAddress, Common.Services.RefreshTokenService.SurfacePlatform);
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
            AccessToken = accessToken.Token,
            RefreshToken = refreshToken.Token,
            ExpiresAt = accessToken.ExpiresAt
        };
    }
}
