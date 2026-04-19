using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Reports.DTOs;
using LogicFit.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Reports.Queries.GetCommissionReport;

public class GetCommissionReportQueryHandler : IRequestHandler<GetCommissionReportQuery, CommissionReportDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly IDateTimeService _dateTimeService;

    public GetCommissionReportQueryHandler(IApplicationDbContext context, ITenantService tenantService, IDateTimeService dateTimeService)
    {
        _context = context;
        _tenantService = tenantService;
        _dateTimeService = dateTimeService;
    }

    public async Task<CommissionReportDto> Handle(GetCommissionReportQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var now = _dateTimeService.UtcNow;
        var from = request.FromDate ?? new DateTime(now.Year, now.Month, 1);
        var to = request.ToDate ?? now;

        var query = _context.Commissions
            .Include(c => c.Employee).ThenInclude(e => e.User)
            .Where(c => c.TenantId == tenantId && c.EarnedDate >= from && c.EarnedDate <= to);

        if (request.EmployeeId.HasValue)
            query = query.Where(c => c.EmployeeId == request.EmployeeId.Value);

        var commissions = await query.ToListAsync(cancellationToken);

        var byEmployee = commissions
            .GroupBy(c => new { c.EmployeeId, Name = c.Employee.User.Email })
            .Select(g => new CommissionByEmployeeDto
            {
                EmployeeId = g.Key.EmployeeId,
                EmployeeName = g.Key.Name,
                TotalEarned = g.Sum(c => c.Amount),
                TotalPaid = g.Where(c => c.Status == CommissionStatus.Paid).Sum(c => c.Amount),
                Pending = g.Where(c => c.Status == CommissionStatus.Pending || c.Status == CommissionStatus.Approved).Sum(c => c.Amount),
                Count = g.Count()
            })
            .OrderByDescending(e => e.TotalEarned)
            .ToList();

        var bySource = commissions
            .GroupBy(c => c.SourceType)
            .Select(g => new CommissionBySourceDto
            {
                SourceType = g.Key.ToString(),
                TotalAmount = g.Sum(c => c.Amount),
                Count = g.Count()
            })
            .OrderByDescending(s => s.TotalAmount)
            .ToList();

        return new CommissionReportDto
        {
            FromDate = from,
            ToDate = to,
            TotalEarned = commissions.Sum(c => c.Amount),
            TotalPaid = commissions.Where(c => c.Status == CommissionStatus.Paid).Sum(c => c.Amount),
            TotalPending = commissions.Where(c => c.Status == CommissionStatus.Pending || c.Status == CommissionStatus.Approved).Sum(c => c.Amount),
            CommissionsCount = commissions.Count,
            ByEmployee = byEmployee,
            BySource = bySource
        };
    }
}
