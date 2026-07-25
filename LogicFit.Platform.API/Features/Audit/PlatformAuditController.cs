using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Platform.API.Features.Audit;

[ApiController]
[Route("api/platform/audit-logs")]
[Authorize(Policy = Permissions.ManagePlatformReports)]
public sealed class PlatformAuditController(IApplicationDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<object>> List(
        [FromQuery] string? entityName = null,
        [FromQuery] string? action = null,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var query = context.AuditLogs.AsNoTracking().IgnoreQueryFilters();
        if (!string.IsNullOrWhiteSpace(entityName)) query = query.Where(x => x.EntityName == entityName);
        if (!string.IsNullOrWhiteSpace(action) && Enum.TryParse<LogicFit.Domain.Enums.AuditAction>(action, true, out var parsed)) query = query.Where(x => x.Action == parsed);
        if (fromUtc.HasValue) query = query.Where(x => x.Timestamp >= fromUtc.Value);
        if (toUtc.HasValue) query = query.Where(x => x.Timestamp < toUtc.Value);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.Timestamp).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return Ok(new { items, total, page, pageSize, totalPages = (int)Math.Ceiling(total / (double)pageSize) });
    }
}
