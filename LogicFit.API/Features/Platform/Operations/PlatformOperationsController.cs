using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Authorization;
using LogicFit.Domain.Enums;
using LogicFit.API.Features.Platform.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.API.Features.Platform.Operations;

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

    [HttpGet("provisioning")]
    public async Task<IActionResult> GetProvisioning(
        [FromQuery] ProvisioningJobStatus? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PlatformPaging.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var tenants = context.Tenants.AsNoTracking();
        var query = context.ProvisioningJobs.AsNoTracking().AsQueryable();
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);

        var projection = query.OrderByDescending(x => x.CreatedAt).Select(job => new
        {
            job.Id,
            job.TenantId,
            TenantName = tenants.Where(tenant => tenant.Id == job.TenantId).Select(tenant => tenant.Name).FirstOrDefault(),
            job.ApplicationRequestId,
            job.DatabaseResourceId,
            job.Status,
            job.AttemptCount,
            job.StartedAtUtc,
            job.CompletedAtUtc,
            job.NextAttemptAtUtc,
            job.LastErrorCode,
            job.IdempotencyKey
        });

        return Ok(await PlatformPaging.CreateAsync(projection, page, pageSize, cancellationToken));
    }
}
