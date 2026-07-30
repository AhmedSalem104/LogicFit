using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Identity.DTOs;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using LogicFit.Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Identity.Commands.IdentitySignIn;

public sealed class IdentitySignInCommandHandler : IRequestHandler<IdentitySignInCommand, IdentitySignInDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IIdentityWorkspaceSessionIssuer _issuer;

    public IdentitySignInCommandHandler(IApplicationDbContext context, IIdentityWorkspaceSessionIssuer issuer)
    {
        _context = context;
        _issuer = issuer;
    }

    public async Task<IdentitySignInDto> Handle(IdentitySignInCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = IdentityEmailAddress.Normalize(request.Email);
        var identity = await _context.IdentityAccounts
            .FirstOrDefaultAsync(x => x.NormalizedEmail == normalizedEmail, cancellationToken);
        if (identity is null || !identity.IsActive || identity.EmailVerifiedAt is null ||
            !BCrypt.Net.BCrypt.Verify(request.Password, identity.PasswordHash))
            throw new UnauthorizedException("Invalid credentials");

        return await _issuer.IssueAsync(identity.Id, cancellationToken);
    }
}
