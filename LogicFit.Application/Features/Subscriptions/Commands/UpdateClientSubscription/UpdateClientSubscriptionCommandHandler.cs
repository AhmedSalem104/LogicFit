using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Subscriptions.Commands.UpdateClientSubscription;

public class UpdateClientSubscriptionCommandHandler : IRequestHandler<UpdateClientSubscriptionCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public UpdateClientSubscriptionCommandHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<bool> Handle(UpdateClientSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var subscription = await _context.ClientSubscriptions
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.Id == request.Id && s.TenantId == tenantId, cancellationToken);

        if (subscription == null)
            throw new NotFoundException("ClientSubscription", request.Id);

        if (request.EndDate.HasValue)
            subscription.EndDate = request.EndDate.Value;

        if (request.Notes != null)
            subscription.Notes = request.Notes;

        if (request.Discount.HasValue)
        {
            subscription.Discount = request.Discount.Value;
            subscription.TotalAmount = subscription.Plan.Price - subscription.Discount;
            if (subscription.TotalAmount < 0) subscription.TotalAmount = 0;
        }

        if (request.AmountPaid.HasValue)
            subscription.AmountPaid = request.AmountPaid.Value;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
