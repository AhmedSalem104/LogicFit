using System.Text.Json;
using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Subscriptions.DTOs;
using LogicFit.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Subscriptions.Queries.GetSubscriptionById;

public class GetSubscriptionByIdQueryHandler : IRequestHandler<GetSubscriptionByIdQuery, ClientSubscriptionDetailDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;

    public GetSubscriptionByIdQueryHandler(IApplicationDbContext context, ITenantService tenantService, ICurrentUserService currentUserService)
    {
        _context = context;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
    }

    public async Task<ClientSubscriptionDetailDto?> Handle(GetSubscriptionByIdQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var currentUserId = Guid.Parse(_currentUserService.UserId!);
        var role = await _context.Users.Where(u => u.Id == currentUserId && u.TenantId == tenantId)
            .Select(u => u.Role).FirstOrDefaultAsync(cancellationToken);

        var subscription = await _context.ClientSubscriptions
            .Include(s => s.Client).ThenInclude(c => c.Profile)
            .Include(s => s.Plan).ThenInclude(p => p.Subscriptions)
            .Include(s => s.SalesCoach).ThenInclude(c => c!.Profile)
            .Include(s => s.Freezes)
            .FirstOrDefaultAsync(s => s.Id == request.Id && s.TenantId == tenantId
                && (role != UserRole.Client || s.ClientId == currentUserId), cancellationToken);

        if (subscription == null) return null;

        // Get renewal history
        var renewalHistory = new List<RenewalHistoryDto>();
        var currentId = subscription.RenewedFromId;
        while (currentId.HasValue)
        {
            var prev = await _context.ClientSubscriptions
                .Include(s => s.Plan)
                .FirstOrDefaultAsync(s => s.Id == currentId.Value, cancellationToken);

            if (prev == null) break;

            renewalHistory.Add(new RenewalHistoryDto
            {
                Id = prev.Id,
                PlanName = prev.Plan.Name,
                StartDate = prev.StartDate,
                EndDate = prev.EndDate,
                Status = prev.Status,
                AmountPaid = prev.AmountPaid
            });

            currentId = prev.RenewedFromId;
        }

        // Deserialize plan features
        List<string> features = new();
        if (!string.IsNullOrEmpty(subscription.Plan.Features))
        {
            try { features = JsonSerializer.Deserialize<List<string>>(subscription.Plan.Features) ?? new(); }
            catch { }
        }

        return new ClientSubscriptionDetailDto
        {
            Id = subscription.Id,
            TenantId = subscription.TenantId,
            ClientId = subscription.ClientId,
            ClientName = subscription.Client.Profile?.FullName ?? subscription.Client.Email,
            ClientPhone = subscription.Client.PhoneNumber,
            ClientEmail = subscription.Client.Email,
            PlanId = subscription.PlanId,
            PlanName = subscription.Plan.Name,
            StartDate = subscription.StartDate,
            EndDate = subscription.EndDate,
            Status = subscription.Status,
            SalesCoachId = subscription.SalesCoachId,
            SalesCoachName = subscription.SalesCoach?.Profile?.FullName ?? subscription.SalesCoach?.Email,
            PaymentMethod = subscription.PaymentMethod,
            TotalAmount = subscription.TotalAmount,
            AmountPaid = subscription.AmountPaid,
            Discount = subscription.Discount,
            Notes = subscription.Notes,
            RenewedFromId = subscription.RenewedFromId,
            TotalFreezeDays = subscription.Freezes.Where(f => !f.IsDeleted).Sum(f => (f.EndDate - f.StartDate).Days),
            Freezes = subscription.Freezes.Where(f => !f.IsDeleted).Select(f => new SubscriptionFreezeDto
            {
                Id = f.Id,
                SubscriptionId = f.SubscriptionId,
                StartDate = f.StartDate,
                EndDate = f.EndDate,
                Reason = f.Reason,
                IsActive = f.IsActive
            }).ToList(),
            PlanDetails = new SubscriptionPlanDto
            {
                Id = subscription.Plan.Id,
                TenantId = subscription.Plan.TenantId,
                Name = subscription.Plan.Name,
                Price = subscription.Plan.Price,
                DurationMonths = subscription.Plan.DurationMonths,
                Description = subscription.Plan.Description,
                Features = features,
                MaxFreezeDays = subscription.Plan.MaxFreezeDays,
                MaxFreezeCount = subscription.Plan.MaxFreezeCount,
                IsActive = subscription.Plan.IsActive,
                SessionsPerWeek = subscription.Plan.SessionsPerWeek,
                InBodyIncluded = subscription.Plan.InBodyIncluded,
                PrivateCoach = subscription.Plan.PrivateCoach,
                ActiveSubscribersCount = subscription.Plan.Subscriptions.Count(s => s.Status == SubscriptionStatus.Active && !s.IsDeleted)
            },
            RenewalHistory = renewalHistory
        };
    }
}
