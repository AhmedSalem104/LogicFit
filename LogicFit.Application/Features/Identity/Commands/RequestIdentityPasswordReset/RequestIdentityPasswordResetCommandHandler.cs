using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Common.Services;
using LogicFit.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Identity.Commands.RequestIdentityPasswordReset;

public sealed class RequestIdentityPasswordResetCommandHandler : IRequestHandler<RequestIdentityPasswordResetCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IdentityEmailActionService _emailActionService;

    public RequestIdentityPasswordResetCommandHandler(IApplicationDbContext context, IdentityEmailActionService emailActionService)
    {
        _context = context;
        _emailActionService = emailActionService;
    }

    public async Task Handle(RequestIdentityPasswordResetCommand request, CancellationToken cancellationToken)
    {
        _emailActionService.EnsureDeliveryAvailable();
        var identity = await _context.IdentityAccounts.SingleOrDefaultAsync(
            x => x.NormalizedEmail == IdentityEmailAddress.Normalize(request.Email), cancellationToken);

        // A caller never learns whether the address exists, is active, or has completed verification.
        if (identity is null || !identity.IsActive || identity.EmailVerifiedAt is null)
            return;

        await _emailActionService.IssueAsync(identity, EmailActionTokenPurpose.PasswordReset, cancellationToken);
    }
}
