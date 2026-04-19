using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Maintenance.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Maintenance.Queries.GetMaintenanceRecords;

public class GetMaintenanceRecordsQueryHandler : IRequestHandler<GetMaintenanceRecordsQuery, List<MaintenanceRecordDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public GetMaintenanceRecordsQueryHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<List<MaintenanceRecordDto>> Handle(GetMaintenanceRecordsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var query = _context.MaintenanceRecords
            .Include(m => m.Equipment)
            .Where(m => m.TenantId == tenantId)
            .AsQueryable();

        if (request.EquipmentId.HasValue)
            query = query.Where(m => m.EquipmentId == request.EquipmentId.Value);

        if (request.Status.HasValue)
            query = query.Where(m => m.Status == request.Status.Value);

        if (request.FromDate.HasValue)
            query = query.Where(m => m.IssueDate >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            query = query.Where(m => m.IssueDate <= request.ToDate.Value);

        var records = await query.OrderByDescending(m => m.IssueDate).ToListAsync(cancellationToken);

        return records.Select(m => new MaintenanceRecordDto
        {
            Id = m.Id,
            TenantId = m.TenantId,
            EquipmentId = m.EquipmentId,
            EquipmentName = m.Equipment.Name,
            IssueDate = m.IssueDate,
            ResolvedDate = m.ResolvedDate,
            Cost = m.Cost,
            Description = m.Description,
            TechnicianName = m.TechnicianName,
            TechnicianContact = m.TechnicianContact,
            Status = m.Status,
            ResolutionNotes = m.ResolutionNotes
        }).ToList();
    }
}
