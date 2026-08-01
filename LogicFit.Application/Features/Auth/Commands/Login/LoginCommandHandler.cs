using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Common.Services;
using LogicFit.Application.Features.Auth.DTOs;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
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
    private readonly ITenantService _tenantService;
    private readonly ITenantAccessGuard _tenantAccessGuard;
    private readonly IIdentityWorkspaceAccessGuard _identityWorkspaceAccessGuard;

    public LoginCommandHandler(
        IApplicationDbContext context,
        IJwtService jwtService,
        IDateTimeService dateTimeService,
        IRbacService rbacService,
        IRefreshTokenService refreshTokenService,
        ICurrentUserService currentUserService,
        ITenantService tenantService,
        ITenantAccessGuard tenantAccessGuard,
        IIdentityWorkspaceAccessGuard identityWorkspaceAccessGuard)
    {
        _context = context;
        _jwtService = jwtService;
        _dateTimeService = dateTimeService;
        _rbacService = rbacService;
        _refreshTokenService = refreshTokenService;
        _currentUserService = currentUserService;
        _tenantService = tenantService;
        _tenantAccessGuard = tenantAccessGuard;
        _identityWorkspaceAccessGuard = identityWorkspaceAccessGuard;
    }

    public async Task<AuthResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // Resolve the gym from subdomain (preferred) or an explicit tenantId.
        var tenantId = await Common.TenantResolver.ResolveAsync(request.TenantId, request.Subdomain, _tenantService);

        // Gate 1: don't issue a token for a gym that isn't allowed to be accessed (suspended / expired /
        // cancelled / archived). PendingApproval is allowed to sign in — the per-request authorization
        // policy limits it to billing/onboarding endpoints.
        var accessState = await _tenantAccessGuard.GetStateAsync(tenantId, cancellationToken);
        if (Common.Services.TenantAccessPolicy.EvaluateHardBlock(accessState) is { } block)
        {
            throw new TenantAccessException(block.Code, block.HttpStatus);
        }

        // Find user by phone number (include profile for FullName)
        var user = await _context.Users
            .IgnoreQueryFilters()
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.TenantId == tenantId && u.PhoneNumber == request.PhoneNumber && !u.IsDeleted,
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

        // Compatibility bridge: existing tenant accounts continue to authenticate exactly as
        // before, while their next successful sign-in creates/links the global identity and an
        // active workspace membership. A conflicting global password never breaks legacy login.
        var wasIdentityLinked = user.IdentityAccountId.HasValue;
        await EnsureIdentityWorkspaceLinkAsync(user, request.Password, cancellationToken);
        if (!wasIdentityLinked && user.IdentityAccountId.HasValue)
            await _context.SaveChangesAsync(cancellationToken);

        var identityAccess = await _identityWorkspaceAccessGuard.EvaluateAsync(user.Id, tenantId, cancellationToken);
        if (identityAccess.Mode == IdentityWorkspaceAccessMode.Blocked)
            throw new TenantAccessException(identityAccess.Code ?? "WORKSPACE_ACCESS_DENIED", 403);

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
            AccessToken = accessToken.Token,
            RefreshToken = refreshToken.Token,
            ExpiresAt = accessToken.ExpiresAt
            ,MustChangePassword = user.MustChangePassword
        };
    }

    private async Task EnsureIdentityWorkspaceLinkAsync(User user, string password, CancellationToken cancellationToken)
    {
        if (user.IdentityAccountId.HasValue)
            return;

        var normalizedEmail = user.Email.Trim().ToUpperInvariant();
        var normalizedPhone = string.IsNullOrWhiteSpace(user.PhoneNumber)
            ? null
            : new string(user.PhoneNumber.Where(char.IsDigit).ToArray());
        var identity = await _context.IdentityAccounts.FirstOrDefaultAsync(x =>
            x.NormalizedEmail == normalizedEmail ||
            (normalizedPhone != null && x.NormalizedPhoneNumber == normalizedPhone), cancellationToken);

        if (identity is null)
        {
            identity = new IdentityAccount
            {
                Email = user.Email,
                NormalizedEmail = normalizedEmail,
                PhoneNumber = user.PhoneNumber,
                NormalizedPhoneNumber = normalizedPhone,
                PasswordHash = user.PasswordHash,
                IsActive = user.IsActive,
                LastLoginAt = _dateTimeService.UtcNow
            };
            _context.IdentityAccounts.Add(identity);
        }
        else
        {
            // Same email/phone with a different password is an unrelated legacy account. It
            // remains tenant-only until it can be merged through an explicit account-recovery flow.
            if (!identity.IsActive || !BCrypt.Net.BCrypt.Verify(password, identity.PasswordHash))
                return;
            identity.LastLoginAt = _dateTimeService.UtcNow;
        }

        var existingMembership = await _context.WorkspaceMemberships.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.UserId == user.Id, cancellationToken);
        if (existingMembership is not null && existingMembership.IdentityAccountId != identity.Id)
            return;

        user.IdentityAccountId = identity.Id;
        if (existingMembership is null)
        {
            _context.WorkspaceMemberships.Add(new WorkspaceMembership
            {
                TenantId = user.TenantId,
                IdentityAccountId = identity.Id,
                UserId = user.Id,
                Role = user.Role,
                Status = WorkspaceMembershipStatus.Active,
                ApprovedAt = _dateTimeService.UtcNow,
                ApprovedBy = "legacy-import"
            });
        }
    }
}
