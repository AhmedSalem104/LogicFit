using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.MembershipCards.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.MembershipCards.Queries.GetMembershipCards;

public class GetMembershipCardsQueryHandler : IRequestHandler<GetMembershipCardsQuery, List<MembershipCardDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public GetMembershipCardsQueryHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<List<MembershipCardDto>> Handle(GetMembershipCardsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var query = _context.MembershipCards
            .Include(c => c.Client)
            .Where(c => c.TenantId == tenantId)
            .AsQueryable();

        if (request.ClientId.HasValue)
            query = query.Where(c => c.ClientId == request.ClientId.Value);

        if (request.IsActive.HasValue)
            query = query.Where(c => c.IsActive == request.IsActive.Value);

        var cards = await query.OrderByDescending(c => c.IssuedAt).ToListAsync(cancellationToken);

        return cards.Select(c => new MembershipCardDto
        {
            Id = c.Id,
            TenantId = c.TenantId,
            ClientId = c.ClientId,
            ClientName = c.Client.Email,
            CardNumber = c.CardNumber,
            QrCode = c.QrCode,
            IsActive = c.IsActive,
            IssuedAt = c.IssuedAt,
            ExpiresAt = c.ExpiresAt,
            RevokedAt = c.RevokedAt,
            RevokedReason = c.RevokedReason
        }).ToList();
    }
}
