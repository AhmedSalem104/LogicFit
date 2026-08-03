using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Platform.Dashboard;
using LogicFit.Domain.Authorization;
using LogicFit.Domain.Enums;
using LogicFit.API.Features.Platform.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.API.Features.Platform.Dashboard;

[ApiController]
[Route("api/platform/dashboard")]
[Authorize(Policy = Permissions.ManagePlatformReports)]
public sealed class PlatformDashboardController(IMediator mediator, IApplicationDbContext context) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PlatformDashboardDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PlatformDashboardDto>> Get([FromQuery] GetPlatformDashboardQuery query)
        => Ok(await mediator.Send(query));

    [HttpGet("tenants")]
    public async Task<IActionResult> Tenants([FromQuery] string? search = null, [FromQuery] TenantStatus? status = null, [FromQuery] Guid? planId = null, [FromQuery] int page = 1, [FromQuery] int pageSize = PlatformPaging.DefaultPageSize, CancellationToken cancellationToken = default)
    {
        var tenants = context.Tenants.AsNoTracking().Where(x => x.Id != PlatformConstants.PlatformTenantId && !x.IsDeleted);
        if (!string.IsNullOrWhiteSpace(search)) tenants = tenants.Where(x => x.Name.Contains(search) || (x.Subdomain != null && x.Subdomain.Contains(search)));
        if (status.HasValue) tenants = tenants.Where(x => x.Status == status.Value);
        var subscriptions = context.TenantSubscriptions.AsNoTracking().IgnoreQueryFilters().Where(x => !x.IsDeleted);
        if (planId.HasValue) subscriptions = subscriptions.Where(x => x.PlanId == planId.Value);
        var mappings = context.TenantDatabaseMappings.AsNoTracking().IgnoreQueryFilters().Where(x => x.IsActive);
        var resources = context.DatabaseResources.AsNoTracking().IgnoreQueryFilters();
        var provisioning = context.ProvisioningJobs.AsNoTracking().IgnoreQueryFilters();
        var backups = context.DatabaseBackups.AsNoTracking().IgnoreQueryFilters();

        var totalCount = await tenants.CountAsync(cancellationToken);
        var tenantRows = await tenants.OrderBy(x => x.Name)
            .Skip((Math.Max(1, page) - 1) * Math.Clamp(pageSize, 1, PlatformPaging.MaximumPageSize))
            .Take(Math.Clamp(pageSize, 1, PlatformPaging.MaximumPageSize))
            .ToListAsync(cancellationToken);
        var tenantIds = tenantRows.Select(x => x.Id).ToArray();

        // Tenant members are still read from the compatibility projection during the data
        // transfer. Keeping this query separate is mandatory: Users is not owned by Platform DB.
        var memberCounts = await context.Users.IgnoreQueryFilters()
            .Where(user => tenantIds.Contains(user.TenantId) && user.Role == UserRole.Client && !user.IsDeleted)
            .GroupBy(user => user.TenantId)
            .Select(group => new { TenantId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(x => x.TenantId, x => x.Count, cancellationToken);

        var subscriptionRows = await subscriptions.Where(x => tenantIds.Contains(x.TenantId))
            .Select(subscription => new
            {
                subscription.TenantId,
                subscription.Status,
                subscription.EndDate,
                subscription.PlanId,
                PlanName = subscription.Plan.Name,
                subscription.CreatedAt
            })
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
        var mappingRows = await mappings.Where(x => tenantIds.Contains(x.TenantId))
            .Join(resources, mapping => mapping.DatabaseResourceId, resource => resource.Id,
                (mapping, resource) => new { mapping.TenantId, resource.Id, resource.Status, resource.AssignedAtUtc, resource.LastHealthCheckAtUtc })
            .ToListAsync(cancellationToken);
        var provisioningRows = await provisioning.Where(x => tenantIds.Contains(x.TenantId))
            .OrderByDescending(x => x.CreatedAt)
            .Select(job => new { job.TenantId, job.Status, job.DatabaseResourceId, job.LastErrorCode, job.CreatedAt })
            .ToListAsync(cancellationToken);
        var backupRows = await backups.Where(x => x.TenantId.HasValue && tenantIds.Contains(x.TenantId.Value))
            .OrderByDescending(x => x.CompletedAtUtc ?? x.StartedAtUtc)
            .Select(backup => new { backup.TenantId, backup.Status, backup.CompletedAtUtc, backup.SizeBytes, backup.StartedAtUtc })
            .ToListAsync(cancellationToken);

        var items = tenantRows.Select(tenant => new
        {
            tenant.Id,
            tenant.Name,
            tenant.Subdomain,
            tenant.Status,
            tenant.CreatedAt,
            MembersCount = memberCounts.GetValueOrDefault(tenant.Id),
            Subscription = subscriptionRows.Where(x => x.TenantId == tenant.Id)
                .Select(x => new { x.Status, x.EndDate, x.PlanId, x.PlanName })
                .FirstOrDefault(),
            DatabaseResource = mappingRows.Where(x => x.TenantId == tenant.Id)
                .Select(x => new { x.Id, x.Status, x.AssignedAtUtc, x.LastHealthCheckAtUtc })
                .FirstOrDefault(),
            Provisioning = provisioningRows.Where(x => x.TenantId == tenant.Id)
                .Select(x => new { x.Status, x.DatabaseResourceId, x.LastErrorCode })
                .FirstOrDefault(),
            Backup = backupRows.Where(x => x.TenantId == tenant.Id)
                .Select(x => new { x.Status, x.CompletedAtUtc, x.SizeBytes })
                .FirstOrDefault()
        }).ToList();

        var normalizedPage = Math.Max(1, page);
        var normalizedPageSize = Math.Clamp(pageSize, 1, PlatformPaging.MaximumPageSize);
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)normalizedPageSize);
        return Ok(new PlatformPage<object>(
            items.Cast<object>().ToList(),
            totalCount,
            normalizedPage,
            normalizedPageSize,
            totalPages));
    }
}
