using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Subscriptions.Commands.CreateClientSubscription;

public class CreateClientSubscriptionCommandHandler : IRequestHandler<CreateClientSubscriptionCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;

    public CreateClientSubscriptionCommandHandler(
        IApplicationDbContext context,
        ITenantService tenantService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(CreateClientSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var plan = await _context.SubscriptionPlans
            .FirstOrDefaultAsync(p => p.Id == request.PlanId && p.TenantId == tenantId, cancellationToken);

        if (plan == null)
            throw new NotFoundException("SubscriptionPlan", request.PlanId);

        var subscription = new ClientSubscription
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ClientId = request.ClientId,
            PlanId = request.PlanId,
            StartDate = request.StartDate,
            EndDate = request.StartDate.AddMonths(plan.DurationMonths),
            Status = SubscriptionStatus.Active,
            SalesCoachId = Guid.Parse(_currentUserService.UserId!)
        };

        _context.ClientSubscriptions.Add(subscription);
        await _context.SaveChangesAsync(cancellationToken);

        return subscription.Id;
    }
}
