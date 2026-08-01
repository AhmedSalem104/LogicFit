using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Common.Services;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Identity.Commands.ResetIdentityPassword;

public sealed class ResetIdentityPasswordCommandHandler : IRequestHandler<ResetIdentityPasswordCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeService _dateTimeService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IdentityEmailActionService _emailActionService;

    public ResetIdentityPasswordCommandHandler(
        IApplicationDbContext context,
        IDateTimeService dateTimeService,
        ICurrentUserService currentUserService,
        IRefreshTokenService refreshTokenService,
        IdentityEmailActionService emailActionService)
    {
        _context = context;
        _dateTimeService = dateTimeService;
        _currentUserService = currentUserService;
        _refreshTokenService = refreshTokenService;
        _emailActionService = emailActionService;
    }

    public async Task Handle(ResetIdentityPasswordCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.BeginTransactionAsync(cancellationToken);
        try
        {
            var actionToken = await _emailActionService.ConsumeAsync(
                request.Token,
                EmailActionTokenPurpose.PasswordReset,
                cancellationToken);
            var identity = await _context.IdentityAccounts
                .SingleOrDefaultAsync(x => x.Id == actionToken.IdentityAccountId, cancellationToken)
                ?? throw new DomainException("This password reset link is invalid, expired, or has already been used.");

            if (!identity.IsActive || identity.EmailVerifiedAt is null)
                throw new DomainException("This password reset link is invalid, expired, or has already been used.");

            var newHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            identity.PasswordHash = newHash;

            var linkedUsers = await _context.Users
                .IgnoreQueryFilters()
                .Where(x => x.IdentityAccountId == identity.Id && !x.IsDeleted)
                .ToListAsync(cancellationToken);
            foreach (var user in linkedUsers)
            {
                user.PasswordHash = newHash;
                user.MustChangePassword = false;
                await _refreshTokenService.RevokeAllAsync(user.Id, _currentUserService.IpAddress, cancellationToken);
            }

            var identitySessions = await _context.IdentityWorkspaceSessions
                .Where(x => x.IdentityAccountId == identity.Id && x.RevokedAt == null && x.ExpiresAt > _dateTimeService.UtcNow)
                .ToListAsync(cancellationToken);
            foreach (var session in identitySessions)
                session.RevokedAt = _dateTimeService.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new DomainException("This password reset link is invalid, expired, or has already been used.");
        }
    }
}
