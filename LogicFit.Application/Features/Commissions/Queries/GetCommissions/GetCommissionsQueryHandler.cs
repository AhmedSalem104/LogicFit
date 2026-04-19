using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Commissions.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Commissions.Queries.GetCommissions;

public class GetCommissionsQueryHandler : IRequestHandler<GetCommissionsQuery, List<CommissionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public GetCommissionsQueryHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<List<CommissionDto>> Handle(GetCommissionsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var query = _context.Commissions
            .Include(c => c.Employee).ThenInclude(e => e.User)
            .Where(c => c.TenantId == tenantId).AsQueryable();

        if (request.EmployeeId.HasValue)
            query = query.Where(c => c.EmployeeId == request.EmployeeId.Value);
        if (request.Status.HasValue)
            query = query.Where(c => c.Status == request.Status.Value);
        if (request.SourceType.HasValue)
            query = query.Where(c => c.SourceType == request.SourceType.Value);
        if (request.FromDate.HasValue)
            query = query.Where(c => c.EarnedDate >= request.FromDate.Value);
        if (request.ToDate.HasValue)
            query = query.Where(c => c.EarnedDate <= request.ToDate.Value);

        var commissions = await query.OrderByDescending(c => c.EarnedDate).ToListAsync(cancellationToken);

        return commissions.Select(c => new CommissionDto
        {
            Id = c.Id,
            EmployeeId = c.EmployeeId,
            EmployeeName = c.Employee.User.Email,
            SourceType = c.SourceType,
            ReferenceId = c.ReferenceId,
            Amount = c.Amount,
            SourceAmount = c.SourceAmount,
            EarnedDate = c.EarnedDate,
            Status = c.Status,
            PayrollItemId = c.PayrollItemId,
            Description = c.Description
        }).ToList();
    }
}
