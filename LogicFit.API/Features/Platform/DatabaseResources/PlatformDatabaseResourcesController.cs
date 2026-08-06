using LogicFit.Application.Common.Interfaces;
using LogicFit.API.Features.Platform.Common;
using LogicFit.Domain.Authorization;
using LogicFit.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.API.Features.Platform.DatabaseResources;

/// <summary>
/// Read-only resource-pool view for the Platform console. Database names and connection
/// material are intentionally absent; the resolver remains the only component that can use them.
/// </summary>
[ApiController]
[Route("api/platform/database-resources")]
[Authorize(Policy = Permissions.ManagePlatformBackups)]
public sealed class PlatformDatabaseResourcesController(IApplicationDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PlatformPage<PlatformDatabaseResourceDto>>> List(
        [FromQuery] DatabaseResourceStatus? status = null,
        [FromQuery] Guid? tenantId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PlatformPaging.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var tenants = context.Tenants.AsNoTracking();
        var query = context.DatabaseResources.AsNoTracking().AsQueryable();
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);
        if (tenantId.HasValue) query = query.Where(x => x.ReservedForTenantId == tenantId.Value);

        var projection = query
            .OrderBy(x => x.Status)
            .ThenBy(x => x.CreatedAt)
            .Select(resource => new PlatformDatabaseResourceDto
            {
                Id = resource.Id,
                Provider = resource.Provider,
                HasProtectedConnection = resource.EncryptedConnectionString != null &&
                    resource.EncryptedConnectionString != string.Empty,
                Status = resource.Status,
                TenantId = resource.ReservedForTenantId,
                TenantName = resource.ReservedForTenantId.HasValue
                    ? tenants.Where(tenant => tenant.Id == resource.ReservedForTenantId.Value).Select(tenant => tenant.Name).FirstOrDefault()
                    : null,
                ReservedAtUtc = resource.ReservedAtUtc,
                AssignedAtUtc = resource.AssignedAtUtc,
                LastHealthCheckAtUtc = resource.LastHealthCheckAtUtc,
                SizeBytes = resource.SizeBytes,
                SchemaVersion = resource.SchemaVersion
            });

        return Ok(await PlatformPaging.CreateAsync(projection, page, pageSize, cancellationToken));
    }
}

public sealed class PlatformDatabaseResourceDto
{
    public Guid Id { get; init; }
    public string Provider { get; init; } = string.Empty;
    public bool HasProtectedConnection { get; init; }
    public DatabaseResourceStatus Status { get; init; }
    public Guid? TenantId { get; init; }
    public string? TenantName { get; init; }
    public DateTime? ReservedAtUtc { get; init; }
    public DateTime? AssignedAtUtc { get; init; }
    public DateTime? LastHealthCheckAtUtc { get; init; }
    public long? SizeBytes { get; init; }
    public string? SchemaVersion { get; init; }
}
