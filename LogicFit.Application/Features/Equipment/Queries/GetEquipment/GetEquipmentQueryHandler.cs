using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Equipment.DTOs;
using LogicFit.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Equipment.Queries.GetEquipment;

public class GetEquipmentQueryHandler : IRequestHandler<GetEquipmentQuery, List<EquipmentDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public GetEquipmentQueryHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<List<EquipmentDto>> Handle(GetEquipmentQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var query = _context.Equipment
            .Include(e => e.Branch)
            .Include(e => e.Room)
            .Include(e => e.MaintenanceRecords)
            .Where(e => e.TenantId == tenantId)
            .AsQueryable();

        if (request.BranchId.HasValue)
            query = query.Where(e => e.BranchId == request.BranchId.Value);

        if (request.RoomId.HasValue)
            query = query.Where(e => e.RoomId == request.RoomId.Value);

        if (request.Status.HasValue)
            query = query.Where(e => e.Status == request.Status.Value);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim();
            query = query.Where(e =>
                e.Name.Contains(term) ||
                (e.SerialNumber != null && e.SerialNumber.Contains(term)) ||
                (e.Brand != null && e.Brand.Contains(term)) ||
                (e.Model != null && e.Model.Contains(term)));
        }

        var items = await query.OrderBy(e => e.Name).ToListAsync(cancellationToken);

        return items.Select(e => new EquipmentDto
        {
            Id = e.Id,
            TenantId = e.TenantId,
            BranchId = e.BranchId,
            BranchName = e.Branch.Name,
            RoomId = e.RoomId,
            RoomName = e.Room?.Name,
            Name = e.Name,
            SerialNumber = e.SerialNumber,
            Brand = e.Brand,
            Model = e.Model,
            Category = e.Category,
            PurchaseDate = e.PurchaseDate,
            PurchasePrice = e.PurchasePrice,
            Status = e.Status,
            WarrantyUntil = e.WarrantyUntil,
            ImageUrl = e.ImageUrl,
            Notes = e.Notes,
            OpenMaintenanceCount = e.MaintenanceRecords.Count(m => m.Status != MaintenanceStatus.Completed && m.Status != MaintenanceStatus.Cancelled && !m.IsDeleted)
        }).ToList();
    }
}
