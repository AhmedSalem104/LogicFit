using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Branches.DTOs;
using LogicFit.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Branches.Queries.GetBranches;

public class GetBranchesQueryHandler : IRequestHandler<GetBranchesQuery, List<BranchDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly IDateTimeService _dateTimeService;

    public GetBranchesQueryHandler(IApplicationDbContext context, ITenantService tenantService, IDateTimeService dateTimeService)
    {
        _context = context;
        _tenantService = tenantService;
        _dateTimeService = dateTimeService;
    }

    public async Task<List<BranchDto>> Handle(GetBranchesQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var today = _dateTimeService.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var query = _context.Branches
            .Include(b => b.Manager)
            .Include(b => b.OperatingHours)
            .Where(b => b.TenantId == tenantId)
            .AsQueryable();

        if (request.IsActive.HasValue)
            query = query.Where(b => b.IsActive == request.IsActive.Value);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim();
            query = query.Where(b =>
                b.Name.Contains(term) ||
                (b.Code != null && b.Code.Contains(term)) ||
                (b.City != null && b.City.Contains(term)));
        }

        var branches = await query.OrderByDescending(b => b.IsDefault).ThenBy(b => b.Name).ToListAsync(cancellationToken);

        var result = new List<BranchDto>();
        foreach (var b in branches)
        {
            var activeClients = await _context.ClientSubscriptions
                .CountAsync(s => s.BranchId == b.Id && s.Status == SubscriptionStatus.Active, cancellationToken);

            var todayCheckIns = await _context.Attendances
                .CountAsync(a => a.BranchId == b.Id && a.CheckInTime >= today && a.CheckInTime < tomorrow, cancellationToken);

            result.Add(new BranchDto
            {
                Id = b.Id,
                TenantId = b.TenantId,
                Name = b.Name,
                Code = b.Code,
                Description = b.Description,
                Address = b.Address,
                City = b.City,
                PhoneNumber = b.PhoneNumber,
                Email = b.Email,
                Latitude = b.Latitude,
                Longitude = b.Longitude,
                IsActive = b.IsActive,
                IsDefault = b.IsDefault,
                Capacity = b.Capacity,
                OpenTime = b.OpenTime,
                CloseTime = b.CloseTime,
                ManagerId = b.ManagerId,
                ManagerName = b.Manager?.Email,
                LogoUrl = b.LogoUrl,
                CoverImageUrl = b.CoverImageUrl,
                OperatingHours = b.OperatingHours.Select(h => new BranchOperatingHoursDto
                {
                    Id = h.Id,
                    DayOfWeek = h.DayOfWeek,
                    OpenTime = h.OpenTime,
                    CloseTime = h.CloseTime,
                    IsClosed = h.IsClosed
                }).OrderBy(h => h.DayOfWeek).ToList(),
                ActiveClientsCount = activeClients,
                TodayCheckInsCount = todayCheckIns
            });
        }

        return result;
    }
}
