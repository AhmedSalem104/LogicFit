using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Auth.DTOs;
using LogicFit.Domain.Authorization;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Platform.Auth;

/// <summary>Issues a platform token only after the platform account's linked global identity completed an approved authentication ceremony.</summary>
public sealed class PlatformSessionIssuer : IPlatformSessionIssuer
{
    private readonly IApplicationDbContext _context; private readonly IJwtService _jwt; private readonly IRbacService _rbac;
    private readonly IRefreshTokenService _refresh; private readonly ICurrentUserService _current;
    public PlatformSessionIssuer(IApplicationDbContext context, IJwtService jwt, IRbacService rbac, IRefreshTokenService refresh, ICurrentUserService current)
        => (_context, _jwt, _rbac, _refresh, _current) = (context, jwt, rbac, refresh, current);

    public async Task<AuthResponseDto> IssueAsync(Guid identityAccountId, CancellationToken ct = default)
    {
        var user = await _context.Users.IgnoreQueryFilters().Include(x => x.Profile).SingleOrDefaultAsync(x =>
            x.TenantId == PlatformConstants.PlatformTenantId && x.IdentityAccountId == identityAccountId &&
            (x.Role == Domain.Enums.UserRole.PlatformOwner || x.Role == Domain.Enums.UserRole.PlatformAdmin) && x.IsActive && !x.IsDeleted, ct)
            ?? throw new UnauthorizedException("Invalid credentials");
        var auth = await _rbac.GetUserAuthorizationAsync(user.Id, ct);
        var access = _jwt.GenerateAccessToken(user.Id, user.Email, tenantId: null, auth.Roles, auth.Permissions, user.PermissionsVersion);
        var refresh = _refresh.Issue(user, _current.IpAddress, Common.Services.RefreshTokenService.SurfacePlatform);
        await _context.SaveChangesAsync(ct);
        return new AuthResponseDto { UserId = user.Id, Email = user.Email, PhoneNumber = user.PhoneNumber, FullName = user.Profile?.FullName,
            Role = user.Role.ToString(), Roles = auth.Roles, Permissions = auth.Permissions, TenantId = user.TenantId,
            AccessToken = access.Token, RefreshToken = refresh.Token, ExpiresAt = access.ExpiresAt };
    }
}
