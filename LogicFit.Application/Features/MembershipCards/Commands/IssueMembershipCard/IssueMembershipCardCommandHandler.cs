using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.MembershipCards.Commands.IssueMembershipCard;

public class IssueMembershipCardCommandHandler : IRequestHandler<IssueMembershipCardCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly IDateTimeService _dateTimeService;

    public IssueMembershipCardCommandHandler(IApplicationDbContext context, ITenantService tenantService, IDateTimeService dateTimeService)
    {
        _context = context;
        _tenantService = tenantService;
        _dateTimeService = dateTimeService;
    }

    public async Task<Guid> Handle(IssueMembershipCardCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var clientExists = await _context.Users
            .AnyAsync(u => u.Id == request.ClientId && u.TenantId == tenantId && !u.IsDeleted, cancellationToken);

        if (!clientExists)
            throw new NotFoundException("Client", request.ClientId);

        var existingActive = await _context.MembershipCards
            .Where(c => c.ClientId == request.ClientId && c.TenantId == tenantId && c.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var c in existingActive)
        {
            c.IsActive = false;
            c.RevokedAt = _dateTimeService.UtcNow;
            c.RevokedReason = "Superseded by new card";
        }

        var cardNumber = string.IsNullOrWhiteSpace(request.CardNumber)
            ? GenerateCardNumber()
            : request.CardNumber.Trim();

        if (await _context.MembershipCards.AnyAsync(c => c.TenantId == tenantId && c.CardNumber == cardNumber, cancellationToken))
            throw new ConflictException("Card number already exists");

        var card = new MembershipCard
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ClientId = request.ClientId,
            CardNumber = cardNumber,
            QrCode = Guid.NewGuid().ToString("N"),
            IsActive = true,
            IssuedAt = _dateTimeService.UtcNow,
            ExpiresAt = request.ExpiresAt
        };

        _context.MembershipCards.Add(card);
        await _context.SaveChangesAsync(cancellationToken);

        return card.Id;
    }

    private static string GenerateCardNumber()
    {
        return $"GYM-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper()}";
    }
}
