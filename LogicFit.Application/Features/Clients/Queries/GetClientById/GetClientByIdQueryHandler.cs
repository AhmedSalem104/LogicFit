using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Clients.DTOs;
using LogicFit.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Clients.Queries.GetClientById;

public class GetClientByIdQueryHandler : IRequestHandler<GetClientByIdQuery, ClientDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public GetClientByIdQueryHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<ClientDto?> Handle(GetClientByIdQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        return await _context.Users
            .Include(u => u.Profile)
            .Include(u => u.Subscriptions.Where(s => s.Status == SubscriptionStatus.Active))
                .ThenInclude(s => s.Plan)
            .Where(u => u.Id == request.Id && u.TenantId == tenantId && u.Role == UserRole.Client)
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
            .FirstOrDefaultAsync(cancellationToken);
    }
}
