using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Common.Services;
using LogicFit.Application.Features.Auth.DTOs;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Identity.Commands.SelectIdentityWorkspace;

public sealed class SelectIdentityWorkspaceCommandHandler
    : IRequestHandler<SelectIdentityWorkspaceCommand, AuthResponseDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeService _dateTimeService;
    private readonly IJwtService _jwtService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IRbacService _rbacService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantAccessGuard _tenantAccessGuard;
    private readonly IIdentityWorkspaceAccessGuard _identityWorkspaceAccessGuard;

    public SelectIdentityWorkspaceCommandHandler(
        IApplicationDbContext context,
        IDateTimeService dateTimeService,
        IJwtService jwtService,
        IRefreshTokenService refreshTokenService,
        IRbacService rbacService,
        ICurrentUserService currentUserService,
        ITenantAccessGuard tenantAccessGuard,
        IIdentityWorkspaceAccessGuard identityWorkspaceAccessGuard)
    {
        _context = context;
        _dateTimeService = dateTimeService;
        _jwtService = jwtService;
        _refreshTokenService = refreshTokenService;
        _rbacService = rbacService;
        _currentUserService = currentUserService;
        _tenantAccessGuard = tenantAccessGuard;
        _identityWorkspaceAccessGuard = identityWorkspaceAccessGuard;
    }

    public async Task<AuthResponseDto> Handle(SelectIdentityWorkspaceCommand request, CancellationToken cancellationToken)
    {
        var selectionSession = await IdentityWorkspaceSessionResolver.GetActiveAsync(
            _context, _dateTimeService, request.WorkspaceSelectionToken, cancellationToken);
        var membership = await _context.WorkspaceMemberships.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.IdentityAccountId == selectionSession.IdentityAccountId &&
                                      x.TenantId == request.WorkspaceId &&
                                      x.Status == WorkspaceMembershipStatus.Active && !x.IsDeleted,
                cancellationToken)
            ?? throw new ForbiddenException("This identity does not have an active membership in the selected workspace.");
        var user = await _context.Users
            .IgnoreQueryFilters()
            .Include(x => x.Profile)
            .FirstOrDefaultAsync(x => x.Id == membership.UserId &&
                                      x.TenantId == membership.TenantId &&
                                      x.IsActive && !x.IsDeleted,
                cancellationToken);
        if (user is null)
            throw new ForbiddenException("The workspace account is not active.");

        var identityAccess = await _identityWorkspaceAccessGuard.EvaluateAsync(membership.UserId, request.WorkspaceId, cancellationToken);
        if (identityAccess.Mode == IdentityWorkspaceAccessMode.Blocked)
            throw new TenantAccessException(identityAccess.Code ?? "WORKSPACE_ACCESS_DENIED", 403);

        if (TenantAccessPolicy.EvaluateHardBlock(await _tenantAccessGuard.GetStateAsync(request.WorkspaceId, cancellationToken)) is { } block)
            throw new TenantAccessException(block.Code, block.HttpStatus);

        var auth = await _rbacService.GetUserAuthorizationAsync(membership.UserId, cancellationToken);
        var accessToken = _jwtService.GenerateAccessToken(
            membership.UserId,
            user.Email,
            membership.TenantId,
            auth.Roles,
            auth.Permissions,
            user.PermissionsVersion);
        var refreshToken = _refreshTokenService.Issue(
            user,
            _currentUserService.IpAddress,
            RefreshTokenService.SurfaceTenant);
        await _context.SaveChangesAsync(cancellationToken);

        return new AuthResponseDto
        {
            UserId = membership.UserId,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            FullName = user.Profile?.FullName,
            Role = membership.Role.ToString(),
            Roles = auth.Roles,
            Permissions = auth.Permissions,
            TenantId = membership.TenantId,
            AccessToken = accessToken.Token,
            RefreshToken = refreshToken.Token,
            ExpiresAt = accessToken.ExpiresAt
        };
    }
}
