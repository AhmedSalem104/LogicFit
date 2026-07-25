using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Authorization;
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
    public async Task<IActionResult> GetOutbox([FromQuery] int take = 100, CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 200);
        var items = await context.OutboxMessages.AsNoTracking().OrderByDescending(x => x.OccurredAtUtc).Take(take).ToListAsync(cancellationToken);
        return Ok(items);
    }

    [HttpGet("jobs")]
    public async Task<IActionResult> GetJobs([FromQuery] int take = 100, CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 200);
        var items = await context.JobExecutionLogs.AsNoTracking().OrderByDescending(x => x.StartedAtUtc).Take(take).ToListAsync(cancellationToken);
        return Ok(items);
    }
}
