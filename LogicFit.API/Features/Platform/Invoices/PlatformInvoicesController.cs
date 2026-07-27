using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Authorization;
using LogicFit.API.Features.Platform.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.API.Features.Platform.Invoices;

[ApiController]
[Route("api/platform/invoices")]
[Authorize(Policy = Permissions.ManagePlatformReports)]
public sealed class PlatformInvoicesController(IApplicationDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? number = null, [FromQuery] Guid? tenantId = null, [FromQuery] int page = 1, [FromQuery] int pageSize = PlatformPaging.DefaultPageSize, CancellationToken cancellationToken = default)
    {
        var query = context.Invoices.AsNoTracking().IgnoreQueryFilters();
        if (!string.IsNullOrWhiteSpace(number)) query = query.Where(x => x.InvoiceNumber.Contains(number));
        if (tenantId.HasValue) query = query.Where(x => x.TenantId == tenantId.Value);
        return Ok(await PlatformPaging.CreateAsync(query.OrderByDescending(x => x.IssueDate), page, pageSize, cancellationToken));
    }
}
