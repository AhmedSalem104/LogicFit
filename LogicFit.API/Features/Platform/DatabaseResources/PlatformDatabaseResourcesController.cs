using System.Text.Json;
using LogicFit.API.Features.Platform.Common;
using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Authorization;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.API.Features.Platform.DatabaseResources;

/// <summary>
/// Administrative lifecycle API for the pre-created workspace database pool.
/// Connection strings are accepted only for a write/test operation, protected before they are
/// persisted, and are never returned by this controller.
/// </summary>
[ApiController]
[Route("api/platform/database-resources")]
[Authorize(Policy = Permissions.ManagePlatformBackups)]
public sealed class PlatformDatabaseResourcesController(
    IApplicationDbContext context,
    ApplicationDbContext applicationDb,
    IConnectionStringProtector connectionStringProtector,
    IBackupService backupService,
    TimeProvider timeProvider,
    ICurrentUserService currentUserService,
    IDateTimeService dateTimeService,
    ILogger<PlatformDatabaseResourcesController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PlatformPage<PlatformDatabaseResourceDto>>> List(
        [FromQuery] DatabaseResourceStatus? status = null,
        [FromQuery] Guid? tenantId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PlatformPaging.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, PlatformPaging.MaximumPageSize);

        var query = context.DatabaseResources.AsNoTracking().IgnoreQueryFilters();
        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);
        if (tenantId.HasValue)
        {
            query = query.Where(x => x.ReservedForTenantId == tenantId.Value ||
                context.TenantDatabaseMappings.Any(mapping =>
                    mapping.DatabaseResourceId == x.Id && mapping.TenantId == tenantId.Value && mapping.IsActive));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var resources = await query
            .OrderBy(x => x.Status)
            .ThenBy(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var resourceIds = resources.Select(x => x.Id).ToArray();
        var mappings = await context.TenantDatabaseMappings.AsNoTracking().IgnoreQueryFilters()
            .Where(x => resourceIds.Contains(x.DatabaseResourceId) && x.IsActive)
            .ToListAsync(cancellationToken);
        var tenantIds = mappings.Select(x => x.TenantId)
            .Concat(resources.Where(x => x.ReservedForTenantId.HasValue).Select(x => x.ReservedForTenantId!.Value))
            .Distinct()
            .ToArray();
        var tenants = await context.Tenants.AsNoTracking().IgnoreQueryFilters()
            .Where(x => tenantIds.Contains(x.Id))
            .ToListAsync(cancellationToken);
        var subscriptions = await context.TenantSubscriptions.AsNoTracking().IgnoreQueryFilters()
            .Where(x => tenantIds.Contains(x.TenantId) && !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
        var jobs = await context.ProvisioningJobs.AsNoTracking().IgnoreQueryFilters()
            .Where(x => tenantIds.Contains(x.TenantId))
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
        var backups = await context.DatabaseBackups.AsNoTracking().IgnoreQueryFilters()
            .Where(x => resourceIds.Contains(x.DatabaseResourceId!.Value))
            .OrderByDescending(x => x.CompletedAtUtc ?? x.StartedAtUtc)
            .ToListAsync(cancellationToken);

        var items = resources.Select(resource =>
        {
            var mapping = mappings.FirstOrDefault(x => x.DatabaseResourceId == resource.Id);
            var linkedTenantId = mapping?.TenantId ?? resource.ReservedForTenantId;
            var tenant = linkedTenantId.HasValue
                ? tenants.FirstOrDefault(x => x.Id == linkedTenantId.Value)
                : null;
            var subscription = linkedTenantId.HasValue
                ? subscriptions.FirstOrDefault(x => x.TenantId == linkedTenantId.Value)
                : null;
            var job = linkedTenantId.HasValue
                ? jobs.FirstOrDefault(x => x.TenantId == linkedTenantId.Value)
                : null;
            var resourceBackups = backups.Where(x => x.DatabaseResourceId == resource.Id).ToList();
            var lastBackup = resourceBackups.FirstOrDefault();
            return ToDto(resource, tenant, mapping, subscription, job, resourceBackups.Count, lastBackup);
        }).ToList();

        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);
        return Ok(new PlatformPage<PlatformDatabaseResourceDto>(items, totalCount, page, pageSize, totalPages));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PlatformDatabaseResourceDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var dto = await BuildDtoAsync(id, cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost("test-connection")]
    public async Task<ActionResult<DatabaseConnectionTestDto>> TestConnection(
        [FromBody] DatabaseConnectionTestRequest request,
        CancellationToken cancellationToken)
    {
        var databaseName = request.DatabaseName?.Trim();
        if (!TryNormalizeConnection(request.ConnectionString, databaseName, out var normalized, out var validationError))
            return BadRequest(new { errorCode = "DATABASE_CONNECTION_INVALID", message = validationError });

        var result = await TestSqlConnectionAsync(normalized, databaseName!, cancellationToken);
        return result.Succeeded ? Ok(result) : UnprocessableEntity(result);
    }

    [HttpPost]
    public async Task<ActionResult<PlatformDatabaseResourceDto>> Create(
        [FromBody] CreateDatabaseResourceRequest request,
        CancellationToken cancellationToken)
    {
        var provider = string.IsNullOrWhiteSpace(request.Provider) ? "ManualMonster" : request.Provider.Trim();
        var databaseName = request.DatabaseName?.Trim();
        if (string.IsNullOrWhiteSpace(databaseName) || string.IsNullOrWhiteSpace(request.ConnectionString))
            return BadRequest(new { errorCode = "DATABASE_RESOURCE_FIELDS_REQUIRED", message = "Provider, database name and connection string are required." });

        if (!TryNormalizeConnection(request.ConnectionString, databaseName, out var normalized, out var validationError))
            return BadRequest(new { errorCode = "DATABASE_CONNECTION_INVALID", message = validationError });

        var test = await TestSqlConnectionAsync(normalized, databaseName, cancellationToken);
        if (!test.Succeeded)
            return UnprocessableEntity(test);

        var duplicate = await context.DatabaseResources.IgnoreQueryFilters()
            .AnyAsync(x => x.Provider == provider && x.DatabaseName == databaseName, cancellationToken);
        if (duplicate)
            return Conflict(new { errorCode = "DATABASE_RESOURCE_ALREADY_EXISTS", message = "A resource with this provider and database name already exists." });

        var resource = new DatabaseResource
        {
            Provider = provider,
            DatabaseName = databaseName,
            ServerKey = string.IsNullOrWhiteSpace(request.ServerKey) ? test.ServerKey : request.ServerKey.Trim(),
            EncryptedConnectionString = connectionStringProtector.Protect(normalized),
            Status = DatabaseResourceStatus.Available,
            LastHealthCheckAtUtc = timeProvider.GetUtcNow().UtcDateTime,
            LastError = null
        };
        context.DatabaseResources.Add(resource);
        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("A database resource was registered: {Provider}/{DatabaseName}.", resource.Provider, resource.DatabaseName);

        return CreatedAtAction(nameof(Get), new { id = resource.Id }, await BuildDtoAsync(resource.Id, cancellationToken));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PlatformDatabaseResourceDto>> Update(
        Guid id,
        [FromBody] UpdateDatabaseResourceRequest request,
        CancellationToken cancellationToken)
    {
        var resource = await context.DatabaseResources.IgnoreQueryFilters()
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (resource is null) return NotFound();

        var mappingExists = await context.TenantDatabaseMappings.IgnoreQueryFilters()
            .AnyAsync(x => x.DatabaseResourceId == id && x.IsActive, cancellationToken);
        var locked = mappingExists || resource.Status is DatabaseResourceStatus.Reserved or DatabaseResourceStatus.Provisioning or DatabaseResourceStatus.Assigned;
        var provider = string.IsNullOrWhiteSpace(request.Provider) ? resource.Provider : request.Provider.Trim();
        var databaseName = string.IsNullOrWhiteSpace(request.DatabaseName) ? resource.DatabaseName : request.DatabaseName.Trim();
        var connectionChanged = !string.IsNullOrWhiteSpace(request.ConnectionString);
        var identityChanged = !string.Equals(provider, resource.Provider, StringComparison.Ordinal) ||
            !string.Equals(databaseName, resource.DatabaseName, StringComparison.Ordinal);

        if (locked && (connectionChanged || identityChanged))
            return Conflict(new { errorCode = "DATABASE_RESOURCE_ALLOCATED", message = "An allocated or provisioning resource cannot change its provider, database name, or connection string." });

        string? normalized = null;
        DatabaseConnectionTestDto? test = null;
        if (connectionChanged)
        {
            if (!TryNormalizeConnection(request.ConnectionString!, databaseName, out normalized, out var validationError))
                return BadRequest(new { errorCode = "DATABASE_CONNECTION_INVALID", message = validationError });
            test = await TestSqlConnectionAsync(normalized, databaseName, cancellationToken);
            if (!test.Succeeded) return UnprocessableEntity(test);
        }

        resource.Provider = provider;
        resource.DatabaseName = databaseName;
        if (request.ServerKey is not null)
            resource.ServerKey = string.IsNullOrWhiteSpace(request.ServerKey) ? resource.ServerKey : request.ServerKey.Trim();
        if (normalized is not null)
        {
            resource.ServerKey ??= test!.ServerKey;
            resource.EncryptedConnectionString = connectionStringProtector.Protect(normalized);
            resource.LastHealthCheckAtUtc = timeProvider.GetUtcNow().UtcDateTime;
            resource.LastError = null;
        }

        await context.SaveChangesAsync(cancellationToken);
        return Ok(await BuildDtoAsync(id, cancellationToken));
    }

    [HttpPost("{id:guid}/repair-connection")]
    public async Task<ActionResult<DatabaseResourceOperationDto>> RepairConnection(
        Guid id,
        [FromBody] RepairDatabaseResourceConnectionRequest request,
        CancellationToken cancellationToken)
    {
        if (!request.Confirm)
            return BadRequest(new
            {
                errorCode = "DATABASE_CONNECTION_REPAIR_CONFIRMATION_REQUIRED",
                message = "Explicit confirmation is required before replacing an allocated database connection."
            });

        if (currentUserService.TenantId.HasValue)
            return Forbid();

        var resource = await applicationDb.DatabaseResources
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (resource is null)
            return NotFound();

        var mappings = await applicationDb.TenantDatabaseMappings
            .IgnoreQueryFilters()
            .Where(x => x.DatabaseResourceId == id && x.IsActive)
            .ToListAsync(cancellationToken);
        var isAllocatedRepair = resource.Status == DatabaseResourceStatus.Assigned && mappings.Count > 0;
        var isPoolResourceRepair = mappings.Count == 0 && resource.Status is
            DatabaseResourceStatus.Available or
            DatabaseResourceStatus.Faulted or
            DatabaseResourceStatus.Maintenance;
        if (!isAllocatedRepair && !isPoolResourceRepair)
            return Conflict(new
            {
                errorCode = "DATABASE_RESOURCE_REPAIR_NOT_ALLOWED",
                message = "An allocated resource must have an active workspace mapping, while an unallocated resource must be Available, Failed, or Disabled before repair."
            });

        if (!TryNormalizeConnection(request.ConnectionString, resource.DatabaseName, out var normalized, out var validationError))
        {
            await TryAuditRepairAsync(id, resource.DatabaseName, false, "DATABASE_CONNECTION_INVALID", cancellationToken);
            return BadRequest(new { errorCode = "DATABASE_CONNECTION_INVALID", message = validationError });
        }

        var test = await TestSqlConnectionAsync(normalized!, resource.DatabaseName, cancellationToken);
        if (!test.Succeeded)
        {
            await TryAuditRepairAsync(id, resource.DatabaseName, false, test.ErrorCode ?? "DATABASE_CONNECTION_TEST_FAILED", cancellationToken);
            return UnprocessableEntity(test);
        }

        var protectedConnection = connectionStringProtector.Protect(normalized!);
        await using var transaction = await applicationDb.Database.BeginTransactionAsync(cancellationToken);

        resource.EncryptedConnectionString = protectedConnection;
        resource.ServerKey ??= test.ServerKey;
        resource.LastHealthCheckAtUtc = timeProvider.GetUtcNow().UtcDateTime;
        resource.LastError = null;
        if (isPoolResourceRepair)
        {
            resource.Status = DatabaseResourceStatus.Available;
            resource.ReservedForTenantId = null;
            resource.ReservedAtUtc = null;
            resource.AssignedAtUtc = null;
        }
        foreach (var mapping in mappings)
        {
            mapping.EncryptedConnectionString = protectedConnection;
            mapping.LastValidatedAtUtc = dateTimeService.UtcNow;
        }

        applicationDb.AuditLogs.Add(new AuditLog
        {
            Action = AuditAction.Update,
            EntityName = "DatabaseResource",
            EntityId = id.ToString(),
            NewValues = JsonSerializer.Serialize(new
            {
                Event = "DatabaseResourceConnectionRepaired",
                resource.DatabaseName,
                ActiveMappingCount = mappings.Count,
                ReleasedToPool = isPoolResourceRepair,
                NewStatus = resource.Status.ToString(),
                test.ServerKey
            }),
            AffectedColumns = "EncryptedConnectionString,LastValidatedAtUtc,LastHealthCheckAtUtc",
            Timestamp = dateTimeService.UtcNow,
            UserId = currentUserService.UserId,
            IpAddress = currentUserService.IpAddress,
            UserAgent = currentUserService.UserAgent
        });

        await applicationDb.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation(
            "A database connection was repaired for resource {ResourceId}; status is {Status} and {MappingCount} active mapping(s) remain.",
            id,
            resource.Status,
            mappings.Count);

        return Ok(new DatabaseResourceOperationDto(
            true,
            isPoolResourceRepair
                ? "The connection was tested, protected, and the resource is now Available for a new workspace."
                : "The connection was tested and protected mapping values were repaired successfully.",
            null,
            resource.SchemaVersion));
    }

    [HttpPost("{id:guid}/status")]
    public async Task<ActionResult<PlatformDatabaseResourceDto>> SetStatus(
        Guid id,
        [FromBody] SetDatabaseResourceStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryMapAdminStatus(request.Status, out var targetStatus))
            return BadRequest(new { errorCode = "DATABASE_RESOURCE_STATUS_INVALID", message = "Status must be Available, Disabled, Failed, or Allocated." });

        var resource = await context.DatabaseResources.IgnoreQueryFilters()
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (resource is null) return NotFound();

        var mapping = await context.TenantDatabaseMappings.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.DatabaseResourceId == id && x.IsActive, cancellationToken);
        var reserved = resource.ReservedForTenantId.HasValue || resource.Status is DatabaseResourceStatus.Reserved or DatabaseResourceStatus.Provisioning;

        if (targetStatus == DatabaseResourceStatus.Assigned)
        {
            if (resource.Status == DatabaseResourceStatus.Assigned && mapping is not null)
                return Ok(await BuildDtoAsync(id, cancellationToken));
            return Conflict(new { errorCode = "DATABASE_RESOURCE_LIFECYCLE_CONTROLLED", message = "Allocated is assigned automatically during workspace provisioning." });
        }
        if (mapping is not null || reserved)
            return Conflict(new { errorCode = "DATABASE_RESOURCE_LINKED", message = "Detach the active workspace and finish any reservation before disabling or re-enabling this resource." });
        if (targetStatus == DatabaseResourceStatus.Available && string.IsNullOrWhiteSpace(resource.EncryptedConnectionString))
            return Conflict(new { errorCode = "DATABASE_CONNECTION_NOT_CONFIGURED", message = "Add and test a connection string before marking the resource Available." });

        resource.Status = targetStatus;
        resource.ReservedForTenantId = null;
        resource.ReservedAtUtc = null;
        resource.AssignedAtUtc = null;
        resource.LastError = targetStatus == DatabaseResourceStatus.Available ? null : resource.LastError;
        await context.SaveChangesAsync(cancellationToken);
        return Ok(await BuildDtoAsync(id, cancellationToken));
    }

    [HttpPost("{id:guid}/migrations")]
    public async Task<ActionResult<DatabaseResourceOperationDto>> RunMigrations(Guid id, CancellationToken cancellationToken)
    {
        var resource = await context.DatabaseResources.IgnoreQueryFilters()
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (resource is null) return NotFound();
        if (string.IsNullOrWhiteSpace(resource.EncryptedConnectionString))
            return Conflict(new { errorCode = "DATABASE_CONNECTION_NOT_CONFIGURED", message = "This resource has no protected connection string." });

        var mapping = await context.TenantDatabaseMappings.AsNoTracking().IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.DatabaseResourceId == id && x.IsActive, cancellationToken);
        var tenantId = mapping?.TenantId ?? Guid.NewGuid();
        try
        {
            var connectionString = connectionStringProtector.Unprotect(resource.EncryptedConnectionString);
            var options = new DbContextOptionsBuilder<TenantDbContext>();
            DbContextSqlServerOptions.UseTenantDatabase(options, connectionString);
            await using var tenantDb = new TenantDbContext(options.Options, tenantId);
            await tenantDb.Database.MigrateAsync(cancellationToken);
            if (!await tenantDb.Database.CanConnectAsync(cancellationToken))
                throw new InvalidOperationException("The database did not pass the connectivity check after migrations.");

            resource.LastHealthCheckAtUtc = timeProvider.GetUtcNow().UtcDateTime;
            resource.SchemaVersion = TenantDbContext.MigrationsAssemblyName;
            resource.LastError = null;
            if (resource.Status == DatabaseResourceStatus.Faulted && mapping is null)
                resource.Status = DatabaseResourceStatus.Available;
            await context.SaveChangesAsync(cancellationToken);
            return Ok(new DatabaseResourceOperationDto(true, "Database migrations completed and the connection is healthy.", null, resource.SchemaVersion));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Database migrations failed for resource {ResourceId}.", id);
            resource.LastHealthCheckAtUtc = timeProvider.GetUtcNow().UtcDateTime;
            resource.LastError = "DATABASE_MIGRATION_FAILED";
            if (mapping is null && resource.Status != DatabaseResourceStatus.Assigned)
                resource.Status = DatabaseResourceStatus.Faulted;
            await context.SaveChangesAsync(cancellationToken);
            return UnprocessableEntity(new DatabaseResourceOperationDto(false, "Database migrations failed. Inspect the server log and connection settings.", "DATABASE_MIGRATION_FAILED", null));
        }
    }

    [HttpPost("{id:guid}/backup")]
    public async Task<ActionResult<BackupBatchDto>> Backup(Guid id, CancellationToken cancellationToken)
    {
        var resource = await context.DatabaseResources.AsNoTracking().IgnoreQueryFilters()
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (resource is null) return NotFound();
        var mapping = await context.TenantDatabaseMappings.AsNoTracking().IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.DatabaseResourceId == id && x.IsActive, cancellationToken);
        if (mapping is null || resource.Status != DatabaseResourceStatus.Assigned)
            return Conflict(new { errorCode = "DATABASE_RESOURCE_NOT_ALLOCATED", message = "A backup can be started after the resource is allocated to a workspace." });

        try
        {
            var request = new BackupBatchRequest(
                BackupScope.SelectedTenants,
                new[] { mapping.TenantId },
                $"resource:{id:N}:{timeProvider.GetUtcNow():yyyyMMddHHmmssfff}");
            return Ok(await backupService.CreateBatchAsync(request, cancellationToken));
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { errorCode = "DATABASE_BACKUP_FAILED", message = exception.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var resource = await context.DatabaseResources.IgnoreQueryFilters()
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (resource is null) return NotFound();

        var activeMapping = await context.TenantDatabaseMappings.AsNoTracking().IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.DatabaseResourceId == id && x.IsActive, cancellationToken);
        if (activeMapping is not null)
            return Conflict(new { errorCode = "DATABASE_RESOURCE_LINKED", message = "This resource is linked to an active workspace. Detach the workspace safely before deletion.", tenantId = activeMapping.TenantId });
        if (await context.TenantDatabaseMappings.AsNoTracking().IgnoreQueryFilters().AnyAsync(x => x.DatabaseResourceId == id, cancellationToken))
            return Conflict(new { errorCode = "DATABASE_RESOURCE_HAS_HISTORY", message = "This resource has historical workspace mappings and cannot be deleted. Mark it Disabled instead." });
        if (resource.ReservedForTenantId.HasValue || resource.Status is DatabaseResourceStatus.Reserved or DatabaseResourceStatus.Provisioning or DatabaseResourceStatus.Assigned)
            return Conflict(new { errorCode = "DATABASE_RESOURCE_IN_USE", message = "This resource is reserved or allocated and cannot be deleted." });

        context.DatabaseResources.Remove(resource);
        await context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task<PlatformDatabaseResourceDto?> BuildDtoAsync(Guid id, CancellationToken cancellationToken)
    {
        var resource = await context.DatabaseResources.AsNoTracking().IgnoreQueryFilters()
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (resource is null) return null;
        var mapping = await context.TenantDatabaseMappings.AsNoTracking().IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.DatabaseResourceId == id && x.IsActive, cancellationToken);
        var tenantId = mapping?.TenantId ?? resource.ReservedForTenantId;
        var tenant = tenantId.HasValue
            ? await context.Tenants.AsNoTracking().IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == tenantId.Value, cancellationToken)
            : null;
        var subscription = tenantId.HasValue
            ? await context.TenantSubscriptions.AsNoTracking().IgnoreQueryFilters().Where(x => x.TenantId == tenantId.Value && !x.IsDeleted).OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync(cancellationToken)
            : null;
        var job = tenantId.HasValue
            ? await context.ProvisioningJobs.AsNoTracking().IgnoreQueryFilters().Where(x => x.TenantId == tenantId.Value).OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync(cancellationToken)
            : null;
        var resourceBackups = await context.DatabaseBackups.AsNoTracking().IgnoreQueryFilters()
            .Where(x => x.DatabaseResourceId == id)
            .OrderByDescending(x => x.CompletedAtUtc ?? x.StartedAtUtc)
            .ToListAsync(cancellationToken);
        return ToDto(resource, tenant, mapping, subscription, job, resourceBackups.Count, resourceBackups.FirstOrDefault());
    }

    private static PlatformDatabaseResourceDto ToDto(
        DatabaseResource resource,
        Tenant? tenant,
        TenantDatabaseMapping? mapping,
        TenantSubscription? subscription,
        ProvisioningJob? job,
        int backupCount,
        DatabaseBackup? lastBackup) => new()
    {
        Id = resource.Id,
        ResourceCode = $"DB-{resource.Id.ToString("N")[..8].ToUpperInvariant()}",
        Provider = resource.Provider,
        Status = resource.Status,
        LifecycleStatus = ToAdminStatus(resource.Status),
        TenantId = mapping?.TenantId ?? resource.ReservedForTenantId,
        TenantName = tenant?.Name,
        WorkspaceType = tenant?.WorkspaceType.ToString(),
        WorkspaceStatus = tenant?.Status,
        SubscriptionStatus = subscription?.Status,
        SubscriptionEndDate = subscription?.EndDate,
        ProvisioningStatus = job?.Status,
        ProvisioningError = job?.LastErrorCode,
        ReservedAtUtc = resource.ReservedAtUtc,
        AssignedAtUtc = resource.AssignedAtUtc,
        LastHealthCheckAtUtc = resource.LastHealthCheckAtUtc,
        SizeBytes = resource.SizeBytes,
        SchemaVersion = resource.SchemaVersion,
        LastError = resource.LastError,
        BackupCount = backupCount,
        LastBackupStatus = lastBackup?.Status.ToString(),
        LastBackupCompletedAtUtc = lastBackup?.CompletedAtUtc,
        HasConnectionString = !string.IsNullOrWhiteSpace(resource.EncryptedConnectionString)
    };

    private async Task<DatabaseConnectionTestDto> TestSqlConnectionAsync(string connectionString, string expectedDatabaseName, CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT DB_NAME();";
            var actualDatabaseName = Convert.ToString(await command.ExecuteScalarAsync(cancellationToken));
            if (!string.Equals(actualDatabaseName, expectedDatabaseName, StringComparison.OrdinalIgnoreCase))
                return new DatabaseConnectionTestDto(false, expectedDatabaseName, connection.DataSource, "DATABASE_NAME_MISMATCH", "The connection opened, but it points to a different database.");
            return new DatabaseConnectionTestDto(true, actualDatabaseName ?? expectedDatabaseName, connection.DataSource, null, "Connection succeeded.");
        }
        catch (Exception exception) when (exception is SqlException or InvalidOperationException or ArgumentException or TimeoutException)
        {
            logger.LogWarning("Database connection test failed for {DatabaseName} ({ExceptionType}).", expectedDatabaseName, exception.GetType().Name);
            return new DatabaseConnectionTestDto(false, expectedDatabaseName, null, "DATABASE_CONNECTION_FAILED", "The database connection could not be opened. Check the server, credentials, encryption settings, and firewall.");
        }
    }

    private static bool TryNormalizeConnection(string? connectionString, string? expectedDatabaseName, out string normalized, out string error)
    {
        normalized = string.Empty;
        error = string.Empty;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            error = "A connection string is required.";
            return false;
        }
        if (string.IsNullOrWhiteSpace(expectedDatabaseName))
        {
            error = "A database name is required.";
            return false;
        }
        try
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            builder.ConnectTimeout = Math.Clamp(builder.ConnectTimeout, 1, 30);
            if (string.IsNullOrWhiteSpace(builder.DataSource))
            {
                error = "The connection string does not contain a SQL Server address.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(builder.InitialCatalog))
            {
                error = "The connection string does not contain an initial catalog.";
                return false;
            }
            if (!string.Equals(builder.InitialCatalog.Trim(), expectedDatabaseName.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                error = "The database name must match the connection string initial catalog.";
                return false;
            }
            normalized = builder.ConnectionString;
            return true;
        }
        catch (ArgumentException)
        {
            error = "The connection string format is invalid.";
            return false;
        }
    }

    private static bool TryMapAdminStatus(string? value, out DatabaseResourceStatus status)
    {
        status = default;
        if (string.IsNullOrWhiteSpace(value)) return false;
        if (value.Equals("Available", StringComparison.OrdinalIgnoreCase)) { status = DatabaseResourceStatus.Available; return true; }
        if (value.Equals("Disabled", StringComparison.OrdinalIgnoreCase) || value.Equals("Maintenance", StringComparison.OrdinalIgnoreCase)) { status = DatabaseResourceStatus.Maintenance; return true; }
        if (value.Equals("Failed", StringComparison.OrdinalIgnoreCase) || value.Equals("Faulted", StringComparison.OrdinalIgnoreCase)) { status = DatabaseResourceStatus.Faulted; return true; }
        if (value.Equals("Allocated", StringComparison.OrdinalIgnoreCase) || value.Equals("Assigned", StringComparison.OrdinalIgnoreCase)) { status = DatabaseResourceStatus.Assigned; return true; }
        return false;
    }

    private static string ToAdminStatus(DatabaseResourceStatus status) => status switch
    {
        DatabaseResourceStatus.Available => "Available",
        DatabaseResourceStatus.Assigned => "Allocated",
        DatabaseResourceStatus.Faulted => "Failed",
        DatabaseResourceStatus.Maintenance or DatabaseResourceStatus.Retired => "Disabled",
        DatabaseResourceStatus.Reserved or DatabaseResourceStatus.Provisioning => "Provisioning",
        DatabaseResourceStatus.RestorePending => "RestorePending",
        _ => status.ToString()
    };

    private async Task TryAuditRepairAsync(
        Guid resourceId,
        string databaseName,
        bool succeeded,
        string errorCode,
        CancellationToken cancellationToken)
    {
        try
        {
            applicationDb.AuditLogs.Add(new AuditLog
            {
                Action = AuditAction.Update,
                EntityName = "DatabaseResource",
                EntityId = resourceId.ToString(),
                NewValues = JsonSerializer.Serialize(new
                {
                    Event = "DatabaseResourceConnectionRepairAttempt",
                    databaseName,
                    Success = succeeded,
                    ErrorCode = errorCode
                }),
                AffectedColumns = "EncryptedConnectionString",
                Timestamp = dateTimeService.UtcNow,
                UserId = currentUserService.UserId,
                IpAddress = currentUserService.IpAddress,
                UserAgent = currentUserService.UserAgent
            });
            await applicationDb.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Could not write the database connection repair audit entry for {ResourceId}.", resourceId);
        }
    }
}

public sealed class PlatformDatabaseResourceDto
{
    public Guid Id { get; init; }
    public string ResourceCode { get; init; } = string.Empty;
    public string Provider { get; init; } = string.Empty;
    public DatabaseResourceStatus Status { get; init; }
    public string LifecycleStatus { get; init; } = string.Empty;
    public Guid? TenantId { get; init; }
    public string? TenantName { get; init; }
    public string? WorkspaceType { get; init; }
    public TenantStatus? WorkspaceStatus { get; init; }
    public TenantSubscriptionStatus? SubscriptionStatus { get; init; }
    public DateTime? SubscriptionEndDate { get; init; }
    public ProvisioningJobStatus? ProvisioningStatus { get; init; }
    public string? ProvisioningError { get; init; }
    public DateTime? ReservedAtUtc { get; init; }
    public DateTime? AssignedAtUtc { get; init; }
    public DateTime? LastHealthCheckAtUtc { get; init; }
    public long? SizeBytes { get; init; }
    public string? SchemaVersion { get; init; }
    public string? LastError { get; init; }
    public int BackupCount { get; init; }
    public string? LastBackupStatus { get; init; }
    public DateTime? LastBackupCompletedAtUtc { get; init; }
    public bool HasConnectionString { get; init; }
}

public sealed record DatabaseConnectionTestRequest(string? DatabaseName, string? ConnectionString);
public sealed record CreateDatabaseResourceRequest(string? Provider, string? DatabaseName, string? ServerKey, string? ConnectionString);
public sealed record UpdateDatabaseResourceRequest(string? Provider, string? DatabaseName, string? ServerKey, string? ConnectionString);
public sealed record RepairDatabaseResourceConnectionRequest(string? ConnectionString, bool Confirm);
public sealed record SetDatabaseResourceStatusRequest(string? Status);
public sealed record DatabaseConnectionTestDto(bool Succeeded, string DatabaseName, string? ServerKey, string? ErrorCode, string Message);
public sealed record DatabaseResourceOperationDto(bool Succeeded, string Message, string? ErrorCode, string? SchemaVersion);
