using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Common.Services;
using LogicFit.Application.Features.Auth.DTOs;
using LogicFit.Domain.Exceptions;
using LogicFit.Domain.Enums;
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
    private readonly IIdentityWorkspaceAccessGuard _identityWorkspaceAccessGuard;

    public RefreshTokenCommandHandler(
        IApplicationDbContext context,
        IJwtService jwtService,
        IDateTimeService dateTimeService,
        IRbacService rbacService,
        IRefreshTokenService refreshTokenService,
        ICurrentUserService currentUserService,
        IIdentityWorkspaceAccessGuard identityWorkspaceAccessGuard)
    {
        _context = context;
        _jwtService = jwtService;
        _dateTimeService = dateTimeService;
        _rbacService = rbacService;
        _refreshTokenService = refreshTokenService;
        _currentUserService = currentUserService;
        _identityWorkspaceAccessGuard = identityWorkspaceAccessGuard;
    }

    public async Task<AuthResponseDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var (user, newToken) = await _refreshTokenService.RotateAsync(
            request.RefreshToken, _currentUserService.IpAddress, request.Surface, cancellationToken);

        if (user.TenantId != PlatformConstants.PlatformTenantId)
        {
            var identityAccess = await _identityWorkspaceAccessGuard.EvaluateAsync(user.Id, user.TenantId, cancellationToken);
            if (identityAccess.Mode == IdentityWorkspaceAccessMode.Blocked)
                throw new TenantAccessException(identityAccess.Code ?? "WORKSPACE_ACCESS_DENIED", 403);
        }

        await _context.SaveChangesAsync(cancellationToken);

        WorkspaceType? workspaceType = null;
        if (user.TenantId != PlatformConstants.PlatformTenantId)
        {
            workspaceType = await _context.Tenants
                .IgnoreQueryFilters()
                .Where(x => x.Id == user.TenantId && !x.IsDeleted)
                .Select(x => (WorkspaceType?)x.WorkspaceType)
                .SingleOrDefaultAsync(cancellationToken);
        }

        var auth = workspaceType.HasValue
            ? await _rbacService.GetUserAuthorizationForTenantAsync(user.Id, user.TenantId, cancellationToken)
            : await _rbacService.GetUserAuthorizationAsync(user.Id, cancellationToken);
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
            WorkspaceType = workspaceType,
            Capabilities = workspaceType.HasValue ? WorkspaceCapabilities.For(workspaceType.Value) : Array.Empty<string>(),
            AccessToken = accessToken.Token,
            RefreshToken = newToken.Token,
            ExpiresAt = accessToken.ExpiresAt
            ,MustChangePassword = user.MustChangePassword
        };
    }
}
