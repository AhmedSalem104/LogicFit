using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Shifts.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Shifts.Queries.GetShiftAssignments;

public class GetShiftAssignmentsQueryHandler : IRequestHandler<GetShiftAssignmentsQuery, List<ShiftAssignmentDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public GetShiftAssignmentsQueryHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<List<ShiftAssignmentDto>> Handle(GetShiftAssignmentsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var query = _context.ShiftAssignments
            .Include(a => a.Shift)
            .Include(a => a.Employee).ThenInclude(e => e.User)
            .Where(a => a.TenantId == tenantId)
            .AsQueryable();

        if (request.EmployeeId.HasValue)
            query = query.Where(a => a.EmployeeId == request.EmployeeId.Value);
        if (request.ShiftId.HasValue)
            query = query.Where(a => a.ShiftId == request.ShiftId.Value);
        if (request.FromDate.HasValue)
            query = query.Where(a => a.Date >= request.FromDate.Value.Date);
        if (request.ToDate.HasValue)
            query = query.Where(a => a.Date <= request.ToDate.Value.Date);

        var items = await query.OrderBy(a => a.Date).ToListAsync(cancellationToken);

        return items.Select(a => new ShiftAssignmentDto
        {
            Id = a.Id,
            ShiftId = a.ShiftId,
            ShiftName = a.Shift.Name,
            EmployeeId = a.EmployeeId,
            EmployeeName = a.Employee.User.Email,
            Date = a.Date,
            ActualCheckIn = a.ActualCheckIn,
            ActualCheckOut = a.ActualCheckOut,
            Notes = a.Notes
        }).ToList();
    }
}
