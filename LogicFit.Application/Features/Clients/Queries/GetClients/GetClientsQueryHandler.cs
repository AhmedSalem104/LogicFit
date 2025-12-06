using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Clients.DTOs;
using LogicFit.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Clients.Queries.GetClients;

public class GetClientsQueryHandler : IRequestHandler<GetClientsQuery, List<ClientDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public GetClientsQueryHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<List<ClientDto>> Handle(GetClientsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var query = _context.Users
            .Include(u => u.Profile)
            .Include(u => u.Subscriptions.Where(s => s.Status == SubscriptionStatus.Active))
                .ThenInclude(s => s.Plan)
            .Where(u => u.TenantId == tenantId && u.Role == UserRole.Client)
            .AsQueryable();

        if (request.IsActive.HasValue)
            query = query.Where(u => u.IsActive == request.IsActive.Value);

        if (!string.IsNullOrEmpty(request.SearchTerm))
            query = query.Where(u =>
                (u.PhoneNumber != null && u.PhoneNumber.Contains(request.SearchTerm)) ||
                (u.Email != null && u.Email.Contains(request.SearchTerm)) ||
                (u.Profile != null && u.Profile.FullName != null && u.Profile.FullName.Contains(request.SearchTerm)));

        return await query
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new ClientDto
            {
                Id = u.Id,
                TenantId = u.TenantId,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber ?? string.Empty,
                IsActive = u.IsActive,
                WalletBalance = u.WalletBalance,
                Profile = u.Profile != null ? new ClientProfileDto
                {
                    FullName = u.Profile.FullName,
                    Gender = (int?)u.Profile.Gender,
                    BirthDate = u.Profile.BirthDate,
                    HeightCm = u.Profile.HeightCm,
                    ActivityLevel = u.Profile.ActivityLevel,
                    MedicalHistory = u.Profile.MedicalHistory
                } : null,
                ActiveSubscription = u.Subscriptions.Where(s => s.Status == SubscriptionStatus.Active).Select(s => new ClientSubscriptionInfoDto
                {
                    Id = s.Id,
                    PlanName = s.Plan.Name,
                    StartDate = s.StartDate,
                    EndDate = s.EndDate,
                    Status = s.Status.ToString()
                }).FirstOrDefault()
            })
            .ToListAsync(cancellationToken);
    }
}
