using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Reports.DTOs;
using LogicFit.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Reports.Queries.GetEquipmentUtilizationReport;

public class GetEquipmentUtilizationReportQueryHandler : IRequestHandler<GetEquipmentUtilizationReportQuery, EquipmentUtilizationReportDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public GetEquipmentUtilizationReportQueryHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<EquipmentUtilizationReportDto> Handle(GetEquipmentUtilizationReportQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var query = _context.Equipment
            .Include(e => e.Branch)
            .Include(e => e.MaintenanceRecords)
            .Where(e => e.TenantId == tenantId);

        if (request.BranchId.HasValue)
            query = query.Where(e => e.BranchId == request.BranchId.Value);

        var equipment = await query.ToListAsync(cancellationToken);

        var mostCostly = equipment
            .Select(e => new EquipmentCostDto
            {
                EquipmentId = e.Id,
                Name = e.Name,
                MaintenanceRecordsCount = e.MaintenanceRecords.Count(m => !m.IsDeleted),
                TotalMaintenanceCost = e.MaintenanceRecords.Where(m => !m.IsDeleted).Sum(m => m.Cost ?? 0),
                PurchasePrice = e.PurchasePrice
            })
            .Where(e => e.TotalMaintenanceCost > 0 || e.MaintenanceRecordsCount > 0)
            .OrderByDescending(e => e.TotalMaintenanceCost)
            .Take(10)
            .ToList();

        var byBranch = equipment
            .GroupBy(e => new { e.BranchId, e.Branch.Name })
            .Select(g => new EquipmentByBranchDto
            {
                BranchId = g.Key.BranchId,
                BranchName = g.Key.Name,
                Total = g.Count(),
                Active = g.Count(e => e.Status == EquipmentStatus.Active),
                UnderMaintenance = g.Count(e => e.Status == EquipmentStatus.UnderMaintenance),
                OutOfService = g.Count(e => e.Status == EquipmentStatus.OutOfService)
            })
            .OrderByDescending(b => b.Total)
            .ToList();

        return new EquipmentUtilizationReportDto
        {
            TotalEquipment = equipment.Count,
            ActiveCount = equipment.Count(e => e.Status == EquipmentStatus.Active),
            UnderMaintenanceCount = equipment.Count(e => e.Status == EquipmentStatus.UnderMaintenance),
            OutOfServiceCount = equipment.Count(e => e.Status == EquipmentStatus.OutOfService),
            TotalPurchaseValue = equipment.Sum(e => e.PurchasePrice ?? 0),
            TotalMaintenanceCost = equipment.SelectMany(e => e.MaintenanceRecords).Where(m => !m.IsDeleted).Sum(m => m.Cost ?? 0),
            TotalMaintenanceRecords = equipment.SelectMany(e => e.MaintenanceRecords).Count(m => !m.IsDeleted),
            OpenMaintenanceCount = equipment.SelectMany(e => e.MaintenanceRecords)
                .Count(m => !m.IsDeleted && m.Status != MaintenanceStatus.Completed && m.Status != MaintenanceStatus.Cancelled),
            MostCostlyEquipment = mostCostly,
            ByBranch = byBranch
        };
    }
}
