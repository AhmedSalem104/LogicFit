using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.GateAccess.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.GateAccess.Queries.GetGateAccessLogs;

public class GetGateAccessLogsQueryHandler : IRequestHandler<GetGateAccessLogsQuery, List<GateAccessLogDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public GetGateAccessLogsQueryHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<List<GateAccessLogDto>> Handle(GetGateAccessLogsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var query = _context.GateAccessLogs
            .Include(l => l.Client)
            .Include(l => l.Branch)
            .Where(l => l.TenantId == tenantId)
            .AsQueryable();

        if (request.ClientId.HasValue)
            query = query.Where(l => l.ClientId == request.ClientId.Value);

        if (request.BranchId.HasValue)
            query = query.Where(l => l.BranchId == request.BranchId.Value);

        if (request.Result.HasValue)
            query = query.Where(l => l.Result == request.Result.Value);

        if (request.FromDate.HasValue)
            query = query.Where(l => l.AccessTime >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            query = query.Where(l => l.AccessTime <= request.ToDate.Value);

        var take = request.Take <= 0 ? 200 : Math.Min(request.Take, 1000);

        var logs = await query
            .OrderByDescending(l => l.AccessTime)
            .Take(take)
            .ToListAsync(cancellationToken);

        return logs.Select(l => new GateAccessLogDto
        {
            Id = l.Id,
            ClientId = l.ClientId,
            ClientName = l.Client?.Email,
            BranchId = l.BranchId,
            BranchName = l.Branch?.Name,
            AccessTime = l.AccessTime,
            Result = l.Result,
            Method = l.Method,
            DenyReason = l.DenyReason,
            Notes = l.Notes,
            ScannedCode = l.ScannedCode
        }).ToList();
    }
}
