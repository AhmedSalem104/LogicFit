using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Identity.Commands.RegisterIdentity;

public sealed class RegisterIdentityCommandHandler : IRequestHandler<RegisterIdentityCommand>
{
    private readonly IApplicationDbContext _context;

    public RegisterIdentityCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task Handle(RegisterIdentityCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        var normalizedPhone = string.IsNullOrWhiteSpace(request.PhoneNumber)
            ? null
            : new string(request.PhoneNumber.Where(char.IsDigit).ToArray());
        var exists = await _context.IdentityAccounts.AnyAsync(x =>
            x.NormalizedEmail == normalizedEmail ||
            (normalizedPhone != null && x.NormalizedPhoneNumber == normalizedPhone), cancellationToken);
        if (exists)
            throw new ConflictException("An identity already exists with these credentials.");

        _context.IdentityAccounts.Add(new IdentityAccount
        {
            Email = request.Email.Trim(),
            NormalizedEmail = normalizedEmail,
            PhoneNumber = request.PhoneNumber?.Trim(),
            NormalizedPhoneNumber = normalizedPhone,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            IsActive = true
        });
        await _context.SaveChangesAsync(cancellationToken);
    }
}
