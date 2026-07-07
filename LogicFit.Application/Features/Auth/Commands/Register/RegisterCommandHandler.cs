using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Auth.DTOs;
using LogicFit.Domain.Authorization;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Auth.Commands.Register;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponseDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly IDateTimeService _dateTimeService;
    private readonly IRbacService _rbacService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ICurrentUserService _currentUserService;

    public RegisterCommandHandler(
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

    public async Task<AuthResponseDto> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        // Check if tenant exists
        var tenantExists = await _context.Tenants
            .AnyAsync(t => t.Id == request.TenantId, cancellationToken);

        if (!tenantExists)
        {
            throw new NotFoundException(nameof(Tenant), request.TenantId);
        }

        // Check if email already exists for this tenant
        var emailExists = await _context.Users
            .AnyAsync(u => u.TenantId == request.TenantId && u.Email == request.Email, cancellationToken);

        if (emailExists)
        {
            throw new ValidationException("Email", "Email already registered for this gym");
        }

        // Check if phone already exists for this tenant
        if (!string.IsNullOrEmpty(request.PhoneNumber))
        {
            var phoneExists = await _context.Users
                .AnyAsync(u => u.TenantId == request.TenantId && u.PhoneNumber == request.PhoneNumber, cancellationToken);

            if (phoneExists)
            {
                throw new ValidationException("PhoneNumber", "Phone number already registered for this gym");
            }
        }

        // Public registration ALWAYS creates a Client (no privilege escalation via request body).
        var user = new User
        {
            TenantId = request.TenantId,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = UserRole.Client,
            IsActive = true
        };

        _context.Users.Add(user);

        // Create user profile if full name provided
        if (!string.IsNullOrEmpty(request.FullName))
        {
            var profile = new UserProfile
            {
                UserId = user.Id,
                FullName = request.FullName
            };
            _context.UserProfiles.Add(profile);
        }

        // Assign the Client system role (RBAC source of truth)
        await _rbacService.EnsureUserInRoleAsync(user.Id, user.TenantId, SystemRoles.Client, cancellationToken);

        var refreshToken = _refreshTokenService.Issue(
            user, _currentUserService.IpAddress, Common.Services.RefreshTokenService.SurfaceTenant);

        await _context.SaveChangesAsync(cancellationToken);

        // Resolve roles + permissions for the token
        var auth = await _rbacService.GetUserAuthorizationAsync(user.Id, cancellationToken);

        var accessToken = _jwtService.GenerateAccessToken(
            user.Id, user.Email, user.TenantId, auth.Roles, auth.Permissions, user.PermissionsVersion);

        return new AuthResponseDto
        {
            UserId = user.Id,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            FullName = request.FullName,
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
