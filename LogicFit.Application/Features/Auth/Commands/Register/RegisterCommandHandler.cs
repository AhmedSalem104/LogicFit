using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Auth.DTOs;
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

    public RegisterCommandHandler(
        IApplicationDbContext context,
        IJwtService jwtService,
        IDateTimeService dateTimeService)
    {
        _context = context;
        _jwtService = jwtService;
        _dateTimeService = dateTimeService;
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

        // Create user
        var user = new User
        {
            TenantId = request.TenantId,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = request.Role,
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

        await _context.SaveChangesAsync(cancellationToken);

        // Generate tokens
        var accessToken = _jwtService.GenerateAccessToken(user.Id, user.Email, user.TenantId, user.Role);
        var refreshToken = _jwtService.GenerateRefreshToken();

        return new AuthResponseDto
        {
            UserId = user.Id,
            Email = user.Email,
            Role = user.Role.ToString(),
            TenantId = user.TenantId,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = _dateTimeService.UtcNow.AddMinutes(60)
        };
    }
}
