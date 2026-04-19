using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Branches.DTOs;
using LogicFit.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Branches.Queries.GetBranchById;

public class GetBranchByIdQueryHandler : IRequestHandler<GetBranchByIdQuery, BranchDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly IDateTimeService _dateTimeService;

    public GetBranchByIdQueryHandler(IApplicationDbContext context, ITenantService tenantService, IDateTimeService dateTimeService)
    {
        _context = context;
        _tenantService = tenantService;
        _dateTimeService = dateTimeService;
    }

    public async Task<BranchDto?> Handle(GetBranchByIdQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var b = await _context.Branches
            .Include(x => x.Manager)
            .Include(x => x.OperatingHours)
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.TenantId == tenantId, cancellationToken);

        if (b == null) return null;

        var today = _dateTimeService.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var activeClients = await _context.ClientSubscriptions
            .CountAsync(s => s.BranchId == b.Id && s.Status == SubscriptionStatus.Active, cancellationToken);

        var todayCheckIns = await _context.Attendances
            .CountAsync(a => a.BranchId == b.Id && a.CheckInTime >= today && a.CheckInTime < tomorrow, cancellationToken);

        return new BranchDto
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
        };
    }
}
