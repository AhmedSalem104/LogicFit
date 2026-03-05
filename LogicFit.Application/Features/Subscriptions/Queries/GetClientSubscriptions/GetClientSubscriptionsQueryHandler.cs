using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Subscriptions.DTOs;
using LogicFit.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Subscriptions.Queries.GetClientSubscriptions;

public class GetClientSubscriptionsQueryHandler : IRequestHandler<GetClientSubscriptionsQuery, List<ClientSubscriptionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public GetClientSubscriptionsQueryHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<List<ClientSubscriptionDto>> Handle(GetClientSubscriptionsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var query = _context.ClientSubscriptions
            .Include(s => s.Client).ThenInclude(c => c.Profile)
            .Include(s => s.Plan)
            .Include(s => s.SalesCoach).ThenInclude(c => c!.Profile)
            .Include(s => s.Freezes)
            .Where(s => s.TenantId == tenantId)
            .AsQueryable();

        if (request.ClientId.HasValue)
            query = query.Where(s => s.ClientId == request.ClientId.Value);

        if (request.Status.HasValue)
            query = query.Where(s => s.Status == request.Status.Value);

        if (request.PlanId.HasValue)
            query = query.Where(s => s.PlanId == request.PlanId.Value);

        if (request.ExpiringWithinDays.HasValue)
        {
            var expiryDate = DateTime.UtcNow.AddDays(request.ExpiringWithinDays.Value);
            query = query.Where(s => s.Status == SubscriptionStatus.Active
                && s.EndDate <= expiryDate && s.EndDate > DateTime.UtcNow);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim().ToLower();
            query = query.Where(s =>
                (s.Client.Profile != null && s.Client.Profile.FullName.ToLower().Contains(term))
                || (s.Client.PhoneNumber != null && s.Client.PhoneNumber.Contains(term))
                || (s.Client.Email != null && s.Client.Email.ToLower().Contains(term)));
        }

        return await query
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new ClientSubscriptionDto
            {
                Id = s.Id,
                TenantId = s.TenantId,
                ClientId = s.ClientId,
                ClientName = s.Client.Profile != null ? s.Client.Profile.FullName : s.Client.Email,
                PlanId = s.PlanId,
                PlanName = s.Plan.Name,
                StartDate = s.StartDate,
                EndDate = s.EndDate,
                Status = s.Status,
                SalesCoachId = s.SalesCoachId,
                SalesCoachName = s.SalesCoach != null
                    ? (s.SalesCoach.Profile != null ? s.SalesCoach.Profile.FullName : s.SalesCoach.Email)
                    : null,
                PaymentMethod = s.PaymentMethod,
                TotalAmount = s.TotalAmount,
                AmountPaid = s.AmountPaid,
                Discount = s.Discount,
                Notes = s.Notes,
                RenewedFromId = s.RenewedFromId,
                TotalFreezeDays = s.Freezes.Where(f => !f.IsDeleted).Sum(f => (int)(f.EndDate - f.StartDate).TotalDays),
                Freezes = s.Freezes.Where(f => !f.IsDeleted).Select(f => new SubscriptionFreezeDto
                {
                    Id = f.Id,
                    SubscriptionId = f.SubscriptionId,
                    StartDate = f.StartDate,
                    EndDate = f.EndDate,
                    Reason = f.Reason,
                    IsActive = f.IsActive
                }).ToList()
            })
            .ToListAsync(cancellationToken);
    }
}
