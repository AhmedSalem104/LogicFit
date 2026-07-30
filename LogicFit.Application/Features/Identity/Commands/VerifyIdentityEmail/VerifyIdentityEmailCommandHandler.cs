using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Common.Services;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Identity.Commands.VerifyIdentityEmail;

public sealed class VerifyIdentityEmailCommandHandler : IRequestHandler<VerifyIdentityEmailCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeService _dateTimeService;
    private readonly IdentityEmailActionService _emailActionService;

    public VerifyIdentityEmailCommandHandler(
        IApplicationDbContext context,
        IDateTimeService dateTimeService,
        IdentityEmailActionService emailActionService)
    {
        _context = context;
        _dateTimeService = dateTimeService;
        _emailActionService = emailActionService;
    }

    public async Task Handle(VerifyIdentityEmailCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.BeginTransactionAsync(cancellationToken);
        try
        {
            var actionToken = await _emailActionService.ConsumeAsync(
                request.Token,
                EmailActionTokenPurpose.EmailVerification,
                cancellationToken);
            var identity = await _context.IdentityAccounts
                .SingleOrDefaultAsync(x => x.Id == actionToken.IdentityAccountId, cancellationToken)
                ?? throw new DomainException("This email link is invalid, expired, or has already been used.");

            if (!identity.IsActive)
                throw new DomainException("This email link is invalid, expired, or has already been used.");

            identity.EmailVerifiedAt ??= _dateTimeService.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new DomainException("This email link is invalid, expired, or has already been used.");
        }
    }
}
