using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Common.Services;
using LogicFit.Application.Features.Identity;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Identity.Commands.RegisterIdentity;

public sealed class RegisterIdentityCommandHandler : IRequestHandler<RegisterIdentityCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IdentityEmailActionService _emailActionService;

    public RegisterIdentityCommandHandler(IApplicationDbContext context, IdentityEmailActionService emailActionService)
    {
        _context = context;
        _emailActionService = emailActionService;
    }

    public async Task Handle(RegisterIdentityCommand request, CancellationToken cancellationToken)
    {
        _emailActionService.EnsureDeliveryAvailable();
        var normalizedEmail = IdentityEmailAddress.Normalize(request.Email);
        var normalizedPhone = string.IsNullOrWhiteSpace(request.PhoneNumber)
            ? null
            : OtpService.NormalizePhone(request.PhoneNumber);
        var identity = await _context.IdentityAccounts
            .SingleOrDefaultAsync(x => x.NormalizedEmail == normalizedEmail, cancellationToken);
        if (identity is not null && identity.EmailVerifiedAt is not null)
            return; // Deliberately generic: registration must not reveal whether an account exists.

        if (identity is null)
        {
            identity = new IdentityAccount
            {
                FullName = request.FullName.Trim(),
                Email = request.Email.Trim(),
                NormalizedEmail = normalizedEmail,
                PhoneNumber = normalizedPhone,
                NormalizedPhoneNumber = normalizedPhone,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                IsActive = true
            };
            _context.IdentityAccounts.Add(identity);
            await _context.SaveChangesAsync(cancellationToken);
        }

        await _emailActionService.IssueAsync(identity, EmailActionTokenPurpose.EmailVerification, cancellationToken);
    }
}
