using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Auth.Commands.LogoutAll;

public class LogoutAllCommandHandler : IRequestHandler<LogoutAllCommand, Unit>
{
    private readonly IApplicationDbContext _context;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ICurrentUserService _currentUserService;

    public LogoutAllCommandHandler(
        IApplicationDbContext context,
        IRefreshTokenService refreshTokenService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _refreshTokenService = refreshTokenService;
        _currentUserService = currentUserService;
    }

    public async Task<Unit> Handle(LogoutAllCommand request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(_currentUserService.UserId, out var userId))
        {
            throw new UnauthorizedException("Not authenticated");
        }

        await _refreshTokenService.RevokeAllAsync(userId, _currentUserService.IpAddress, cancellationToken);

        // Invalidate already-issued access tokens on their next refresh.
        var user = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user != null)
        {
            user.PermissionsVersion++;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
