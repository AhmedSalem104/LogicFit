using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Subscriptions.Commands.CreateSubscriptionFreeze;

public class CreateSubscriptionFreezeCommandHandler : IRequestHandler<CreateSubscriptionFreezeCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public CreateSubscriptionFreezeCommandHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<Guid> Handle(CreateSubscriptionFreezeCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var subscription = await _context.ClientSubscriptions
            .FirstOrDefaultAsync(s => s.Id == request.SubscriptionId && s.TenantId == tenantId, cancellationToken);

        if (subscription == null)
            throw new NotFoundException("ClientSubscription", request.SubscriptionId);

        var freezeDays = (request.EndDate - request.StartDate).Days;
        subscription.EndDate = subscription.EndDate.AddDays(freezeDays);
        subscription.Status = SubscriptionStatus.Suspended;

        var freeze = new SubscriptionFreeze
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SubscriptionId = request.SubscriptionId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Reason = request.Reason
        };

        _context.SubscriptionFreezes.Add(freeze);
        await _context.SaveChangesAsync(cancellationToken);

        return freeze.Id;
    }
}
