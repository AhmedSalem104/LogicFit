using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.MembershipCards.Commands.RevokeMembershipCard;

public class RevokeMembershipCardCommandHandler : IRequestHandler<RevokeMembershipCardCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly IDateTimeService _dateTimeService;

    public RevokeMembershipCardCommandHandler(IApplicationDbContext context, ITenantService tenantService, IDateTimeService dateTimeService)
    {
        _context = context;
        _tenantService = tenantService;
        _dateTimeService = dateTimeService;
    }

    public async Task Handle(RevokeMembershipCardCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var card = await _context.MembershipCards
            .FirstOrDefaultAsync(c => c.Id == request.Id && c.TenantId == tenantId, cancellationToken)
            ?? throw new NotFoundException("MembershipCard", request.Id);

        card.IsActive = false;
        card.RevokedAt = _dateTimeService.UtcNow;
        card.RevokedReason = request.Reason;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
