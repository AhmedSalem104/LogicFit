using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Leaves.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Leaves.Queries.GetLeaveRequests;

public class GetLeaveRequestsQueryHandler : IRequestHandler<GetLeaveRequestsQuery, List<LeaveRequestDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public GetLeaveRequestsQueryHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<List<LeaveRequestDto>> Handle(GetLeaveRequestsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var query = _context.LeaveRequests
            .Include(l => l.Employee).ThenInclude(e => e.User)
            .Include(l => l.ReviewedBy)
            .Where(l => l.TenantId == tenantId).AsQueryable();

        if (request.EmployeeId.HasValue)
            query = query.Where(l => l.EmployeeId == request.EmployeeId.Value);
        if (request.Status.HasValue)
            query = query.Where(l => l.Status == request.Status.Value);
        if (request.LeaveType.HasValue)
            query = query.Where(l => l.LeaveType == request.LeaveType.Value);
        if (request.FromDate.HasValue)
            query = query.Where(l => l.FromDate >= request.FromDate.Value);
        if (request.ToDate.HasValue)
            query = query.Where(l => l.ToDate <= request.ToDate.Value);

        var items = await query.OrderByDescending(l => l.FromDate).ToListAsync(cancellationToken);

        return items.Select(l => new LeaveRequestDto
        {
            Id = l.Id,
            EmployeeId = l.EmployeeId,
            EmployeeName = l.Employee.User.Email,
            FromDate = l.FromDate,
            ToDate = l.ToDate,
            LeaveType = l.LeaveType,
            Reason = l.Reason,
            Status = l.Status,
            ReviewedById = l.ReviewedById,
            ReviewedByName = l.ReviewedBy?.Email,
            ReviewedAt = l.ReviewedAt,
            ReviewNotes = l.ReviewNotes
        }).ToList();
    }
}
