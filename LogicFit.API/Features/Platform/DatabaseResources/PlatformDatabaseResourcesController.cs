using LogicFit.Application.Common.Interfaces;
using LogicFit.API.Features.Platform.Common;
using LogicFit.Domain.Authorization;
using LogicFit.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.API.Features.Platform.DatabaseResources;

/// <summary>
/// Safe resource-pool view and registration boundary for the Platform console. Database names and
/// connection material are intentionally absent; the resolver remains the only component that can use them.
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

    /// <summary>
    /// Registers an operator-owned database in the pool. The clear connection string is accepted
    /// only on this server boundary, protected immediately, and is never returned or logged.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(PlatformDatabaseResourceDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<PlatformDatabaseResourceDto>> Register(
        [FromBody] RegisterDatabaseResourceRequest request,
        [FromServices] IConnectionStringProtector connectionStringProtector,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.DatabaseName) || request.DatabaseName.Trim().Length > 256)
            return BadRequest(new { message = "DatabaseName is required and must be at most 256 characters." });
        if (string.IsNullOrWhiteSpace(request.ConnectionString) || request.ConnectionString.Length > 4000)
            return BadRequest(new { message = "A protected connection string is required." });
        if (!string.Equals(request.Provider, "ManualMonster", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(request.Provider, "LocalSql", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Provider must be ManualMonster or LocalSql." });

        var databaseName = request.DatabaseName.Trim();
        var exists = await context.DatabaseResources.IgnoreQueryFilters()
            .AnyAsync(x => x.Provider == request.Provider.Trim() && x.DatabaseName == databaseName, cancellationToken);
        if (exists)
            return Conflict(new { message = "This provider/database resource is already registered." });

        var resource = new Domain.Entities.DatabaseResource
        {
            Provider = request.Provider.Trim(),
            DatabaseName = databaseName,
            ServerKey = string.IsNullOrWhiteSpace(request.ServerKey) ? null : request.ServerKey.Trim(),
            EncryptedConnectionString = connectionStringProtector.Protect(request.ConnectionString.Trim()),
            Status = DatabaseResourceStatus.Available
        };
        context.DatabaseResources.Add(resource);
        await context.SaveChangesAsync(cancellationToken);

        return StatusCode(StatusCodes.Status201Created, new PlatformDatabaseResourceDto
        {
            Id = resource.Id,
            Provider = resource.Provider,
            HasProtectedConnection = true,
            Status = resource.Status,
            TenantId = null,
            TenantName = null,
            ReservedAtUtc = resource.ReservedAtUtc,
            AssignedAtUtc = resource.AssignedAtUtc,
            LastHealthCheckAtUtc = resource.LastHealthCheckAtUtc,
            SizeBytes = resource.SizeBytes,
            SchemaVersion = resource.SchemaVersion
        });
    }
}

public sealed class RegisterDatabaseResourceRequest
{
    public string Provider { get; init; } = "ManualMonster";
    public string DatabaseName { get; init; } = string.Empty;
    public string? ServerKey { get; init; }
    public string ConnectionString { get; init; } = string.Empty;
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
