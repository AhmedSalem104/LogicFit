using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Auth.DTOs;
using LogicFit.Domain.Authorization;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Auth.Commands.RefreshToken;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponseDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly IDateTimeService _dateTimeService;
    private readonly IRbacService _rbacService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ICurrentUserService _currentUserService;

    public RefreshTokenCommandHandler(
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

    public async Task<AuthResponseDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var (user, newToken) = await _refreshTokenService.RotateAsync(
            request.RefreshToken, _currentUserService.IpAddress, request.Surface, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        var auth = await _rbacService.GetUserAuthorizationAsync(user.Id, cancellationToken);
        var profile = await _context.UserProfiles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.UserId == user.Id, cancellationToken);

        // Platform users carry no TenantId claim (sentinel tenant => null).
        Guid? tenantClaim = user.TenantId == PlatformConstants.PlatformTenantId ? null : user.TenantId;

        var accessToken = _jwtService.GenerateAccessToken(
            user.Id, user.Email, tenantClaim, auth.Roles, auth.Permissions, user.PermissionsVersion);

        return new AuthResponseDto
        {
            UserId = user.Id,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            FullName = profile?.FullName,
            Role = user.Role.ToString(),
            Roles = auth.Roles,
            Permissions = auth.Permissions,
            TenantId = user.TenantId,
            AccessToken = accessToken.Token,
            RefreshToken = newToken.Token,
            ExpiresAt = accessToken.ExpiresAt
        };
    }
}
