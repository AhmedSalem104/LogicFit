using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Common.Services;
using LogicFit.Application.Features.Auth.DTOs;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using System.Data.Common;
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
    private readonly IWorkspaceDatabaseScope _workspaceDatabaseScope;

    public SelectIdentityWorkspaceCommandHandler(
        IApplicationDbContext context,
        IDateTimeService dateTimeService,
        IJwtService jwtService,
        IRefreshTokenService refreshTokenService,
        IRbacService rbacService,
        ICurrentUserService currentUserService,
        ITenantAccessGuard tenantAccessGuard,
        IIdentityWorkspaceAccessGuard identityWorkspaceAccessGuard,
        IWorkspaceDatabaseScope workspaceDatabaseScope)
    {
        _context = context;
        _dateTimeService = dateTimeService;
        _jwtService = jwtService;
        _refreshTokenService = refreshTokenService;
        _rbacService = rbacService;
        _currentUserService = currentUserService;
        _tenantAccessGuard = tenantAccessGuard;
        _identityWorkspaceAccessGuard = identityWorkspaceAccessGuard;
        _workspaceDatabaseScope = workspaceDatabaseScope;
    }

    public async Task<AuthResponseDto> Handle(SelectIdentityWorkspaceCommand request, CancellationToken cancellationToken)
    {
        var selectionSession = await IdentityWorkspaceSessionResolver.GetActiveAsync(
            _context, _dateTimeService, request.WorkspaceSelectionToken, cancellationToken);
        var membership = await _context.WorkspaceMemberships.IgnoreQueryFilters()
            // WorkspaceMembership and Tenant are platform-owned. User/Profile are tenant-owned
            // and are intentionally not mapped by PlatformDbContext; loading them here caused a
            // valid identity login to fail at the selection step with HTTP 500.
            .Where(x => x.IdentityAccountId == selectionSession.IdentityAccountId &&
                        x.TenantId == request.WorkspaceId &&
                        x.Status == WorkspaceMembershipStatus.Active && !x.IsDeleted)
            .Select(x => new WorkspaceMembershipSnapshot(x.TenantId, x.UserId, x.Role))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new ForbiddenException("This identity does not have an active membership in the selected workspace.");

        if (TenantAccessPolicy.EvaluateHardBlock(await _tenantAccessGuard.GetStateAsync(request.WorkspaceId, cancellationToken)) is { } block)
            throw new TenantAccessException(block.Code, block.HttpStatus);

        if (!await _workspaceDatabaseScope.TryOpenAsync(request.WorkspaceId, cancellationToken))
            throw new TenantAccessException(
                "TENANT_DATABASE_UNAVAILABLE",
                503,
                "Workspace database is not ready. Please try again later.");

        try
        {
            var user = await _context.Users
                .IgnoreQueryFilters()
                .Include(x => x.Profile)
                .FirstOrDefaultAsync(x => x.Id == membership.UserId &&
                                          x.TenantId == request.WorkspaceId &&
                                          !x.IsDeleted,
                    cancellationToken);
            if (user is null)
                throw new TenantAccessException("WORKSPACE_ACCOUNT_NOT_FOUND", 403);
            if (!user.IsActive)
                throw new TenantAccessException("WORKSPACE_ACCOUNT_INACTIVE", 403);

            var identityAccess = await _identityWorkspaceAccessGuard.EvaluateAsync(
                membership.UserId,
                request.WorkspaceId,
                cancellationToken);
            if (identityAccess.Mode == IdentityWorkspaceAccessMode.Blocked)
                throw new TenantAccessException(identityAccess.Code ?? "WORKSPACE_ACCESS_DENIED", 403);

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
                ExpiresAt = accessToken.ExpiresAt,
                MustChangePassword = user.MustChangePassword
            };
        }
        catch (DbException)
        {
            throw new TenantAccessException(
                "TENANT_DATABASE_UNAVAILABLE",
                503,
                "Workspace database is not ready. Please try again later.");
        }
        finally
        {
            _workspaceDatabaseScope.Close();
        }
    }

    private sealed record WorkspaceMembershipSnapshot(Guid TenantId, Guid UserId, UserRole Role);
}
