using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Shifts.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Shifts.Queries.GetShifts;

public class GetShiftsQueryHandler : IRequestHandler<GetShiftsQuery, List<ShiftDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public GetShiftsQueryHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<List<ShiftDto>> Handle(GetShiftsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var query = _context.Shifts.Include(s => s.Branch).Where(s => s.TenantId == tenantId).AsQueryable();

        if (request.BranchId.HasValue)
            query = query.Where(s => s.BranchId == request.BranchId.Value);
        if (request.IsActive.HasValue)
            query = query.Where(s => s.IsActive == request.IsActive.Value);

        var shifts = await query.OrderBy(s => s.StartTime).ToListAsync(cancellationToken);

        return shifts.Select(s => new ShiftDto
        {
            Id = s.Id,
            BranchId = s.BranchId,
            BranchName = s.Branch?.Name,
            Name = s.Name,
            StartTime = s.StartTime,
            EndTime = s.EndTime,
            Color = s.Color,
            IsActive = s.IsActive
        }).ToList();
    }
}
