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
            .Include(s => s.Plan)
            .Include(s => s.Freezes)
            .FirstOrDefaultAsync(s => s.Id == request.SubscriptionId && s.TenantId == tenantId, cancellationToken);

        if (subscription == null)
            throw new NotFoundException("ClientSubscription", request.SubscriptionId);

        if (subscription.Status != SubscriptionStatus.Active)
            throw new ConflictException("Only active subscriptions can be frozen");

        // Validate freeze dates
        if (request.StartDate.Date < DateTime.UtcNow.Date)
            throw new ValidationException("StartDate", "Freeze start date cannot be in the past");

        if (request.EndDate <= request.StartDate)
            throw new ValidationException("EndDate", "Freeze end date must be after start date");

        // Check for overlapping active freezes
        var hasOverlap = subscription.Freezes.Any(f => f.IsActive
            && f.StartDate < request.EndDate && f.EndDate > request.StartDate);

        if (hasOverlap)
            throw new ConflictException("Freeze dates overlap with an existing freeze");

        // Check max freeze count
        var existingFreezeCount = subscription.Freezes.Count(f => !f.IsDeleted);
        if (subscription.Plan.MaxFreezeCount > 0 && existingFreezeCount >= subscription.Plan.MaxFreezeCount)
            throw new ConflictException($"Maximum freeze count ({subscription.Plan.MaxFreezeCount}) reached for this plan");

        // Check max freeze days
        var requestedDays = (request.EndDate - request.StartDate).Days;
        var existingFreezeDays = subscription.Freezes
            .Where(f => !f.IsDeleted)
            .Sum(f => (f.EndDate - f.StartDate).Days);

        if (subscription.Plan.MaxFreezeDays > 0 && (existingFreezeDays + requestedDays) > subscription.Plan.MaxFreezeDays)
        {
            var remainingDays = subscription.Plan.MaxFreezeDays - existingFreezeDays;
            throw new ConflictException($"Maximum freeze days exceeded. You have {Math.Max(0, remainingDays)} days remaining");
        }

        // Create freeze
        var freeze = new SubscriptionFreeze
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SubscriptionId = request.SubscriptionId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Reason = request.Reason,
            IsActive = true
        };

        // Extend subscription end date
        subscription.EndDate = subscription.EndDate.AddDays(requestedDays);

        // If freeze starts today or earlier, set status to Suspended
        if (request.StartDate.Date <= DateTime.UtcNow.Date)
            subscription.Status = SubscriptionStatus.Suspended;

        _context.SubscriptionFreezes.Add(freeze);
        await _context.SaveChangesAsync(cancellationToken);

        return freeze.Id;
    }
}
