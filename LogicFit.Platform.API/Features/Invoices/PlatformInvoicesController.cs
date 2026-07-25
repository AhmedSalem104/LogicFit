using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Platform.API.Features.Invoices;

[ApiController]
[Route("api/platform/invoices")]
[Authorize(Policy = Permissions.ManagePlatformReports)]
public sealed class PlatformInvoicesController(IApplicationDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? number = null, [FromQuery] Guid? tenantId = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 200);
        var query = context.Invoices.AsNoTracking().IgnoreQueryFilters();
        if (!string.IsNullOrWhiteSpace(number)) query = query.Where(x => x.InvoiceNumber.Contains(number));
        if (tenantId.HasValue) query = query.Where(x => x.TenantId == tenantId.Value);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.IssueDate).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return Ok(new { items, total, page, pageSize, totalPages = (int)Math.Ceiling(total / (double)pageSize) });
    }
}
