using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Subscriptions.Commands.CancelSubscription;

public class CancelSubscriptionCommandHandler : IRequestHandler<CancelSubscriptionCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public CancelSubscriptionCommandHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<bool> Handle(CancelSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var subscription = await _context.ClientSubscriptions
            .FirstOrDefaultAsync(s => s.Id == request.SubscriptionId && s.TenantId == tenantId, cancellationToken);

        if (subscription == null)
            throw new NotFoundException("ClientSubscription", request.SubscriptionId);

        subscription.Status = SubscriptionStatus.Cancelled;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
