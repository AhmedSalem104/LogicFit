using System.Text.Json;
using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using MediatR;

namespace LogicFit.Application.Features.Subscriptions.Commands.CreateSubscriptionPlan;

public class CreateSubscriptionPlanCommandHandler : IRequestHandler<CreateSubscriptionPlanCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public CreateSubscriptionPlanCommandHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<Guid> Handle(CreateSubscriptionPlanCommand request, CancellationToken cancellationToken)
    {
        var plan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantService.GetCurrentTenantId(),
            Name = request.Name,
            Price = request.Price,
            DurationMonths = request.DurationMonths,
            Description = request.Description,
            Features = request.Features != null ? JsonSerializer.Serialize(request.Features) : null,
            MaxFreezeDays = request.MaxFreezeDays,
            MaxFreezeCount = request.MaxFreezeCount,
            IsActive = request.IsActive,
            SessionsPerWeek = request.SessionsPerWeek,
            InBodyIncluded = request.InBodyIncluded,
            PrivateCoach = request.PrivateCoach
        };

        _context.SubscriptionPlans.Add(plan);
        await _context.SaveChangesAsync(cancellationToken);

        return plan.Id;
    }
}
