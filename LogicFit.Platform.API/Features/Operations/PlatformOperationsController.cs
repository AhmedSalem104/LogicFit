using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Authorization;
using LogicFit.Platform.API.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Platform.API.Features.Operations;

[ApiController]
[Route("api/platform/operations")]
[Authorize(Policy = Permissions.ManagePlatformReports)]
public sealed class PlatformOperationsController(IApplicationDbContext context) : ControllerBase
{
    [HttpGet("outbox")]
    public async Task<IActionResult> GetOutbox([FromQuery] int page = 1, [FromQuery] int pageSize = PlatformPaging.DefaultPageSize, CancellationToken cancellationToken = default)
    {
        var query = context.OutboxMessages.AsNoTracking().OrderByDescending(x => x.OccurredAtUtc);
        return Ok(await PlatformPaging.CreateAsync(query, page, pageSize, cancellationToken));
    }

    [HttpGet("jobs")]
    public async Task<IActionResult> GetJobs([FromQuery] int page = 1, [FromQuery] int pageSize = PlatformPaging.DefaultPageSize, CancellationToken cancellationToken = default)
    {
        var query = context.JobExecutionLogs.AsNoTracking().OrderByDescending(x => x.StartedAtUtc);
        return Ok(await PlatformPaging.CreateAsync(query, page, pageSize, cancellationToken));
    }
}
