using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Commissions.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Commissions.Queries.GetCommissionRules;

public class GetCommissionRulesQueryHandler : IRequestHandler<GetCommissionRulesQuery, List<CommissionRuleDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public GetCommissionRulesQueryHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<List<CommissionRuleDto>> Handle(GetCommissionRulesQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var query = _context.CommissionRules
            .Include(r => r.Employee).ThenInclude(e => e!.User)
            .Where(r => r.TenantId == tenantId).AsQueryable();

        if (request.IsActive.HasValue)
            query = query.Where(r => r.IsActive == request.IsActive.Value);

        var rules = await query.ToListAsync(cancellationToken);

        return rules.Select(r => new CommissionRuleDto
        {
            Id = r.Id,
            EmployeeId = r.EmployeeId,
            EmployeeName = r.Employee?.User.Email,
            Role = r.Role,
            SourceType = r.SourceType,
            Type = r.Type,
            Value = r.Value,
            MinAmount = r.MinAmount,
            IsActive = r.IsActive
        }).ToList();
    }
}
