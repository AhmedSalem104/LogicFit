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

    public LoginCommandHandler(
        IApplicationDbContext context,
        IJwtService jwtService,
        IDateTimeService dateTimeService)
    {
        _context = context;
        _jwtService = jwtService;
        _dateTimeService = dateTimeService;
    }

    public async Task<AuthResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // Find user by phone number
        var user = await _context.Users
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

        // Generate tokens
        var accessToken = _jwtService.GenerateAccessToken(user.Id, user.Email, user.TenantId, user.Role);
        var refreshToken = _jwtService.GenerateRefreshToken();

        return new AuthResponseDto
        {
            UserId = user.Id,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Role = user.Role.ToString(),
            TenantId = user.TenantId,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = _dateTimeService.UtcNow.AddMinutes(60)
        };
    }
}
