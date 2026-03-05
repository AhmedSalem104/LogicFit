using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Subscriptions.Commands.EndFreezeEarly;

public class EndFreezeEarlyCommandHandler : IRequestHandler<EndFreezeEarlyCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public EndFreezeEarlyCommandHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<bool> Handle(EndFreezeEarlyCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var freeze = await _context.SubscriptionFreezes
            .Include(f => f.Subscription)
            .FirstOrDefaultAsync(f => f.Id == request.FreezeId && f.TenantId == tenantId, cancellationToken);

        if (freeze == null)
            throw new NotFoundException("SubscriptionFreeze", request.FreezeId);

        if (!freeze.IsActive)
            throw new ConflictException("This freeze has already ended");

        // Calculate unused freeze days
        var now = DateTime.UtcNow;
        var unusedDays = Math.Max(0, (freeze.EndDate - now).Days);

        // End the freeze
        freeze.EndDate = now;
        freeze.IsActive = false;

        // Shrink subscription end date by unused days
        if (unusedDays > 0)
            freeze.Subscription.EndDate = freeze.Subscription.EndDate.AddDays(-unusedDays);

        // Reactivate subscription
        if (freeze.Subscription.Status == SubscriptionStatus.Suspended)
            freeze.Subscription.Status = SubscriptionStatus.Active;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
