using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Rooms.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Rooms.Queries.GetRooms;

public class GetRoomsQueryHandler : IRequestHandler<GetRoomsQuery, List<RoomDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public GetRoomsQueryHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<List<RoomDto>> Handle(GetRoomsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var query = _context.Rooms
            .Include(r => r.Branch)
            .Include(r => r.Equipment)
            .Where(r => r.TenantId == tenantId)
            .AsQueryable();

        if (request.BranchId.HasValue)
            query = query.Where(r => r.BranchId == request.BranchId.Value);

        if (request.Type.HasValue)
            query = query.Where(r => r.Type == request.Type.Value);

        if (request.IsActive.HasValue)
            query = query.Where(r => r.IsActive == request.IsActive.Value);

        var rooms = await query.OrderBy(r => r.Branch.Name).ThenBy(r => r.Name).ToListAsync(cancellationToken);

        return rooms.Select(r => new RoomDto
        {
            Id = r.Id,
            TenantId = r.TenantId,
            BranchId = r.BranchId,
            BranchName = r.Branch.Name,
            Name = r.Name,
            Type = r.Type,
            Capacity = r.Capacity,
            Description = r.Description,
            ImageUrl = r.ImageUrl,
            IsActive = r.IsActive,
            EquipmentCount = r.Equipment.Count(e => !e.IsDeleted)
        }).ToList();
    }
}
