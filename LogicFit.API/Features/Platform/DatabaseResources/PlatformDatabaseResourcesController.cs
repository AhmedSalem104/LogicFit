using System.Diagnostics;
using System.Security.Cryptography;
using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Common.Services;
using LogicFit.API.Features.Platform.Common;
using LogicFit.Domain.Authorization;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.API.Features.Platform.DatabaseResources;

/// <summary>
/// Platform-only resource-pool boundary. The console receives safe database metadata and
/// diagnostics, while connection material is accepted, decrypted, and used only on the server.
/// </summary>
[ApiController]
[Route("api/platform/database-resources")]
[Authorize(Policy = Permissions.ManagePlatformBackups)]
public sealed class PlatformDatabaseResourcesController(
    IApplicationDbContext context,
    IConnectionStringProtector connectionStringProtector,
    IBackupService backupService,
    ICurrentUserService currentUser,
    IDateTimeService clock,
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
        var query = context.DatabaseResources.AsNoTracking().AsQueryable();
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);
        if (tenantId.HasValue) query = query.Where(x => x.ReservedForTenantId == tenantId.Value);

        return Ok(await PlatformPaging.CreateAsync(Project(query), page, pageSize, cancellationToken));
    }

    /// <summary>
    /// Registers an operator-owned database in the pool. The clear connection string is accepted
    /// only on this server boundary, protected immediately, and is never returned or logged.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(PlatformDatabaseResourceDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<PlatformDatabaseResourceDto>> Register(
        [FromBody] RegisterDatabaseResourceRequest request,
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
        var connectionString = request.ConnectionString.Trim();
        if (!TryReadServerMetadata(connectionString, out var serverHost, out var serverPort, out var parseError))
            return BadRequest(new { errorCode = "DATABASE_CONNECTION_STRING_INVALID", message = parseError });

        var exists = await context.DatabaseResources.IgnoreQueryFilters()
            .AnyAsync(x => x.Provider == request.Provider.Trim() && x.DatabaseName == databaseName, cancellationToken);
        if (exists)
            return Conflict(new { message = "This provider/database resource is already registered." });

        var resource = new Domain.Entities.DatabaseResource
        {
            Provider = request.Provider.Trim(),
            DatabaseName = databaseName,
            ServerKey = string.IsNullOrWhiteSpace(request.ServerKey) ? null : request.ServerKey.Trim(),
            ServerHost = serverHost,
            ServerPort = serverPort,
            EncryptedConnectionString = connectionStringProtector.Protect(connectionString),
            Status = DatabaseResourceStatus.Faulted
        };

        var probe = await ProbeAsync(databaseName, connectionString, cancellationToken);
        ApplyProbe(resource, probe);
        resource.Status = probe.Succeeded ? DatabaseResourceStatus.Available : DatabaseResourceStatus.Faulted;
        context.DatabaseResources.Add(resource);
        await context.SaveChangesAsync(cancellationToken);

        var result = await ToDtoAsync(resource.Id, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPost("test-connection")]
    public async Task<ActionResult<PlatformConnectionTestDto>> TestConnection(
        [FromBody] TestConnectionRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.DatabaseName))
            return BadRequest(new { errorCode = "DATABASE_NAME_REQUIRED", message = "اسم قاعدة البيانات مطلوب." });
        if (string.IsNullOrWhiteSpace(request.ConnectionString))
            return BadRequest(new { errorCode = "DATABASE_CONNECTION_STRING_REQUIRED", message = "سلسلة الاتصال مطلوبة." });
        if (!TryReadServerMetadata(request.ConnectionString.Trim(), out var serverHost, out var serverPort, out var parseError))
            return Ok(new PlatformConnectionTestDto(false, request.DatabaseName.Trim(), serverHost, serverPort, null,
                "DATABASE_CONNECTION_STRING_INVALID", parseError, null, DateTime.UtcNow));

        var probe = await ProbeAsync(request.DatabaseName.Trim(), request.ConnectionString.Trim(), cancellationToken);
        return Ok(new PlatformConnectionTestDto(
            probe.Succeeded,
            probe.DatabaseName,
            serverHost,
            serverPort,
            probe.ActualDatabaseName,
            probe.ErrorCode,
            probe.Message,
            probe.DurationMs,
            probe.TestedAtUtc));
    }

    [HttpPost("{id:guid}/test-connection")]
    public async Task<ActionResult<PlatformConnectionTestDto>> TestStoredConnection(
        Guid id,
        CancellationToken cancellationToken)
    {
        var resource = await context.DatabaseResources.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (resource is null) return NotFound();
        if (string.IsNullOrWhiteSpace(resource.EncryptedConnectionString))
            return Ok(new PlatformConnectionTestDto(false, resource.DatabaseName, resource.ServerHost, resource.ServerPort,
                null, "DATABASE_CONNECTION_NOT_CONFIGURED", "لا يوجد اتصال محمي لهذا المورد.", null, clock.UtcNow));

        try
        {
            var connectionString = connectionStringProtector.Unprotect(resource.EncryptedConnectionString);
            var probe = await ProbeAsync(resource.DatabaseName, connectionString, cancellationToken);
            ApplyProbe(resource, probe);
            if (probe.Succeeded)
            {
                resource.LastError = null;
                if (resource.Status is DatabaseResourceStatus.Faulted or DatabaseResourceStatus.Maintenance)
                {
                    var hasActiveMapping = await context.TenantDatabaseMappings
                        .AnyAsync(x => x.DatabaseResourceId == resource.Id && x.IsActive, cancellationToken);
                    resource.Status = hasActiveMapping
                        ? DatabaseResourceStatus.Assigned
                        : resource.ReservedForTenantId.HasValue
                            ? DatabaseResourceStatus.Reserved
                            : DatabaseResourceStatus.Available;
                }
            }
            else
            {
                resource.LastError = probe.ErrorCode;
                if (!resource.ReservedForTenantId.HasValue)
                    resource.Status = DatabaseResourceStatus.Faulted;
            }

            await context.SaveChangesAsync(cancellationToken);
            return Ok(ToConnectionTestDto(resource, probe));
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or CryptographicException)
        {
            resource.LastConnectionTestAtUtc = clock.UtcNow;
            resource.LastConnectionTestSucceeded = false;
            resource.LastConnectionErrorCode = "DATABASE_CONNECTION_UNPROTECT_FAILED";
            resource.LastConnectionErrorMessage = "تعذر قراءة الاتصال المحمي على الخادم.";
            resource.LastError = resource.LastConnectionErrorCode;
            await context.SaveChangesAsync(cancellationToken);
            return Ok(new PlatformConnectionTestDto(false, resource.DatabaseName, resource.ServerHost, resource.ServerPort,
                null, resource.LastConnectionErrorCode, resource.LastConnectionErrorMessage, null, resource.LastConnectionTestAtUtc.Value));
        }
    }

    [HttpPost("{id:guid}/repair-connection")]
    public async Task<ActionResult<PlatformResourceOperationDto>> RepairConnection(
        Guid id,
        [FromBody] RepairConnectionRequest request,
        CancellationToken cancellationToken)
    {
        var resource = await context.DatabaseResources.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (resource is null) return NotFound();
        if (!request.Confirm)
            return BadRequest(new { errorCode = "DATABASE_CONNECTION_REPAIR_CONFIRMATION_REQUIRED", message = "يجب تأكيد استبدال اتصال قاعدة البيانات." });
        if (string.IsNullOrWhiteSpace(request.ConnectionString))
            return BadRequest(new { errorCode = "DATABASE_CONNECTION_STRING_REQUIRED", message = "أدخل سلسلة اتصال جديدة." });
        var connectionString = request.ConnectionString.Trim();
        if (!TryReadServerMetadata(connectionString, out var serverHost, out var serverPort, out var parseError))
            return BadRequest(new { errorCode = "DATABASE_CONNECTION_STRING_INVALID", message = parseError });

        var probe = await ProbeAsync(resource.DatabaseName, connectionString, cancellationToken);
        ApplyProbe(resource, probe);
        if (!probe.Succeeded)
        {
            resource.LastError = probe.ErrorCode;
            await context.SaveChangesAsync(cancellationToken);
            return UnprocessableEntity(ToConnectionTestDto(resource, probe));
        }

        var mapping = await context.TenantDatabaseMappings
            .SingleOrDefaultAsync(x => x.DatabaseResourceId == resource.Id && x.IsActive, cancellationToken);
        if (mapping is not null)
        {
            mapping.EncryptedConnectionString = connectionStringProtector.Protect(connectionString);
            mapping.Provider = resource.Provider;
            mapping.LastValidatedAtUtc = clock.UtcNow;
        }

        resource.ServerHost = serverHost;
        resource.ServerPort = serverPort;
        resource.EncryptedConnectionString = connectionStringProtector.Protect(connectionString);
        resource.Status = mapping is not null
            ? DatabaseResourceStatus.Assigned
            : resource.ReservedForTenantId.HasValue
                ? DatabaseResourceStatus.Reserved
                : DatabaseResourceStatus.Available;
        resource.LastError = null;
        await context.SaveChangesAsync(cancellationToken);
        return Ok(new PlatformResourceOperationDto(true, "تم اختبار الاتصال الجديد وحفظه بأمان.", null, resource.SchemaVersion));
    }

    [HttpPost("{id:guid}/migrations")]
    public async Task<ActionResult<PlatformResourceOperationDto>> RunMigrations(
        Guid id,
        CancellationToken cancellationToken)
    {
        var resource = await context.DatabaseResources.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (resource is null) return NotFound();
        if (string.IsNullOrWhiteSpace(resource.EncryptedConnectionString))
            return Conflict(new { errorCode = "DATABASE_CONNECTION_NOT_CONFIGURED", message = "لا يوجد اتصال محمي لهذا المورد." });

        string connectionString;
        try
        {
            connectionString = connectionStringProtector.Unprotect(resource.EncryptedConnectionString);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or CryptographicException)
        {
            resource.LastError = "DATABASE_CONNECTION_UNPROTECT_FAILED";
            await context.SaveChangesAsync(cancellationToken);
            return Conflict(new { errorCode = resource.LastError, message = "تعذر قراءة الاتصال المحمي على الخادم." });
        }

        var probe = await ProbeAsync(resource.DatabaseName, connectionString, cancellationToken);
        ApplyProbe(resource, probe);
        if (!probe.Succeeded)
        {
            resource.LastError = probe.ErrorCode;
            await context.SaveChangesAsync(cancellationToken);
            return UnprocessableEntity(ToConnectionTestDto(resource, probe));
        }

        var mapping = await context.TenantDatabaseMappings
            .SingleOrDefaultAsync(x => x.DatabaseResourceId == resource.Id && x.IsActive, cancellationToken);
        var tenantId = mapping?.TenantId ?? resource.ReservedForTenantId;

        try
        {
            if (tenantId.HasValue)
            {
                var options = new DbContextOptionsBuilder<TenantDbContext>();
                DbContextSqlServerOptions.UseTenantDatabase(options, connectionString);
                await using var tenantDb = new TenantDbContext(options.Options, tenantId.Value);
                await tenantDb.Database.MigrateAsync(cancellationToken);
                if (!await tenantDb.Database.CanConnectAsync(cancellationToken))
                    throw new InvalidOperationException("TENANT_DATABASE_HEALTH_CHECK_FAILED");

                resource.SchemaVersion = TenantDbContext.MigrationsAssemblyName;
                resource.LastHealthCheckAtUtc = clock.UtcNow;
                resource.LastError = null;
                resource.Status = mapping is null ? DatabaseResourceStatus.Reserved : DatabaseResourceStatus.Assigned;
            }
            else
            {
                resource.LastHealthCheckAtUtc = clock.UtcNow;
                resource.LastError = null;
                if (resource.Status is DatabaseResourceStatus.Faulted or DatabaseResourceStatus.Maintenance)
                    resource.Status = DatabaseResourceStatus.Available;
            }

            await context.SaveChangesAsync(cancellationToken);
            return Ok(new PlatformResourceOperationDto(true,
                tenantId.HasValue
                    ? "اكتملت الترحيلات وفحص CanConnect للمورد."
                    : "نجح اتصال المورد. سيُشغّل الخادم ترحيلات مساحة العمل بعد تخصيصه.",
                null,
                resource.SchemaVersion));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Database resource migration failed for ResourceId {ResourceId}.", resource.Id);
            resource.LastError = "TENANT_MIGRATION_FAILED";
            await context.SaveChangesAsync(cancellationToken);
            return Conflict(new { errorCode = resource.LastError, message = "فشلت الترحيلات أو فحص صحة قاعدة البيانات." });
        }
    }

    [HttpPost("{id:guid}/backup")]
    public async Task<ActionResult<BackupBatchDto>> CreateBackup(
        Guid id,
        CancellationToken cancellationToken)
    {
        var resource = await context.DatabaseResources.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (resource is null) return NotFound();
        var mapping = await context.TenantDatabaseMappings.AsNoTracking()
            .SingleOrDefaultAsync(x => x.DatabaseResourceId == id && x.IsActive, cancellationToken);
        var tenantId = mapping?.TenantId ?? resource.ReservedForTenantId;
        if (!tenantId.HasValue || resource.Status != DatabaseResourceStatus.Assigned)
            return Conflict(new { errorCode = "DATABASE_RESOURCE_NOT_ASSIGNED", message = "لا يمكن إنشاء نسخة قبل تخصيص المورد لمساحة عمل جاهزة." });

        try
        {
            var batch = await backupService.CreateBatchAsync(
                new BackupBatchRequest(BackupScope.SelectedTenants, [tenantId.Value],
                    $"manual-resource:{id:N}:{clock.UtcNow:yyyyMMddHHmmss}"),
                cancellationToken);
            return Ok(batch);
        }
        catch (InvalidOperationException exception)
        {
            logger.LogWarning(exception, "Backup request failed for DatabaseResourceId {ResourceId}.", id);
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { errorCode = "BACKUP_SERVICE_UNAVAILABLE", message = "خدمة النسخ الاحتياطي غير جاهزة حاليًا." });
        }
    }

    [HttpPost("{id:guid}/status")]
    public async Task<ActionResult<PlatformDatabaseResourceDto>> SetStatus(
        Guid id,
        [FromBody] SetResourceStatusRequest request,
        CancellationToken cancellationToken)
    {
        var resource = await context.DatabaseResources.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (resource is null) return NotFound();
        if (!Enum.TryParse<DatabaseResourceStatus>(request.Status, true, out var requestedStatus))
        {
            requestedStatus = request.Status.Equals("Disabled", StringComparison.OrdinalIgnoreCase)
                ? DatabaseResourceStatus.Maintenance
                : request.Status.Equals("Failed", StringComparison.OrdinalIgnoreCase)
                    ? DatabaseResourceStatus.Faulted
                    : request.Status.Equals("Available", StringComparison.OrdinalIgnoreCase)
                        ? DatabaseResourceStatus.Available
                        : (DatabaseResourceStatus)0;
        }

        if (requestedStatus is not (DatabaseResourceStatus.Available or DatabaseResourceStatus.Maintenance or DatabaseResourceStatus.Faulted))
            return BadRequest(new { errorCode = "DATABASE_RESOURCE_STATUS_INVALID", message = "الحالة المطلوبة غير مسموحة من شاشة الإدارة." });

        var hasMapping = await context.TenantDatabaseMappings.AnyAsync(x => x.DatabaseResourceId == id && x.IsActive, cancellationToken);
        if (hasMapping || resource.ReservedForTenantId.HasValue)
            return Conflict(new { errorCode = "DATABASE_RESOURCE_IN_USE", message = "لا يمكن تغيير حالة مورد مخصص لمساحة عمل من هذه الشاشة." });

        resource.Status = requestedStatus;
        if (requestedStatus == DatabaseResourceStatus.Available)
            resource.LastError = null;
        await context.SaveChangesAsync(cancellationToken);
        return Ok(await ToDtoAsync(id, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var resource = await context.DatabaseResources.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (resource is null) return NotFound();

        if (await context.TenantDatabaseMappings.AnyAsync(x => x.DatabaseResourceId == id && x.IsActive, cancellationToken))
            return Conflict(new { errorCode = "DATABASE_RESOURCE_ASSIGNED", message = "لا يمكن حذف مورد مرتبط حاليًا بمساحة عمل." });
        if (await context.ProvisioningJobs.AnyAsync(x => x.DatabaseResourceId == id &&
            (x.Status == ProvisioningJobStatus.Pending || x.Status == ProvisioningJobStatus.AwaitingDatabaseCapacity || x.Status == ProvisioningJobStatus.Provisioning), cancellationToken))
            return Conflict(new { errorCode = "DATABASE_RESOURCE_PROVISIONING", message = "لا يمكن حذف مورد توجد له عملية تجهيز نشطة." });
        if (await context.RestoreJobs.AnyAsync(x => x.TargetDatabaseResourceId == id &&
            (x.Status == RestoreJobStatus.Pending || x.Status == RestoreJobStatus.Running), cancellationToken))
            return Conflict(new { errorCode = "DATABASE_RESOURCE_RESTORE_ACTIVE", message = "لا يمكن حذف مورد توجد له عملية استعادة نشطة." });
        if (await context.DatabaseBackups.AnyAsync(x => x.DatabaseResourceId == id, cancellationToken))
            return Conflict(new { errorCode = "DATABASE_RESOURCE_HAS_BACKUPS", message = "لا يمكن حذف المورد لأن له نسخًا احتياطية مرتبطة بسجل التدقيق." });
        if (resource.ReservedForTenantId.HasValue || resource.Status == DatabaseResourceStatus.Reserved ||
            resource.Status == DatabaseResourceStatus.Provisioning || resource.Status == DatabaseResourceStatus.Assigned)
            return Conflict(new { errorCode = "DATABASE_RESOURCE_RESERVED", message = "يجب تحرير المورد من مساحة العمل قبل حذفه." });

        context.DatabaseResources.Remove(resource);
        SecurityAuditLog.Add(context, currentUser, clock, "PlatformDatabaseResourceDeleted", true, resource.Id);
        await context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private IQueryable<PlatformDatabaseResourceDto> Project(IQueryable<DatabaseResource> source)
    {
        var tenants = context.Tenants.AsNoTracking();
        var mappings = context.TenantDatabaseMappings.AsNoTracking();
        var jobs = context.ProvisioningJobs.AsNoTracking();
        var backups = context.DatabaseBackups.AsNoTracking();
        var restores = context.RestoreJobs.AsNoTracking();
        var subscriptions = context.TenantSubscriptions.AsNoTracking();

        return source
            .OrderBy(x => x.Status)
            .ThenBy(x => x.CreatedAt)
            .Select(resource => new PlatformDatabaseResourceDto
            {
                Id = resource.Id,
                ResourceCode = resource.DatabaseName,
                DatabaseName = resource.DatabaseName,
                Provider = resource.Provider,
                ServerKey = resource.ServerKey,
                ServerHost = resource.ServerHost,
                ServerPort = resource.ServerPort,
                HasProtectedConnection = !string.IsNullOrEmpty(resource.EncryptedConnectionString),
                Status = resource.Status,
                LifecycleStatus = resource.Status == DatabaseResourceStatus.Available ? "Available"
                    : resource.Status == DatabaseResourceStatus.Assigned ? "Allocated"
                    : resource.Status == DatabaseResourceStatus.Reserved || resource.Status == DatabaseResourceStatus.Provisioning ? "Provisioning"
                    : resource.Status == DatabaseResourceStatus.RestorePending ? "RestorePending"
                    : resource.Status == DatabaseResourceStatus.Maintenance ? "Disabled"
                    : resource.Status == DatabaseResourceStatus.Faulted ? "Failed" : "Retired",
                TenantId = resource.ReservedForTenantId,
                TenantName = resource.ReservedForTenantId.HasValue
                    ? tenants.Where(tenant => tenant.Id == resource.ReservedForTenantId.Value).Select(tenant => tenant.Name).FirstOrDefault()
                    : null,
                WorkspaceType = resource.ReservedForTenantId.HasValue
                    ? tenants.Where(tenant => tenant.Id == resource.ReservedForTenantId.Value).Select(tenant => (WorkspaceType?)tenant.WorkspaceType).FirstOrDefault()
                    : null,
                WorkspaceStatus = resource.ReservedForTenantId.HasValue
                    ? tenants.Where(tenant => tenant.Id == resource.ReservedForTenantId.Value).Select(tenant => (TenantStatus?)tenant.Status).FirstOrDefault()
                    : null,
                SubscriptionStatus = resource.ReservedForTenantId.HasValue
                    ? subscriptions.Where(subscription => subscription.TenantId == resource.ReservedForTenantId.Value)
                        .OrderByDescending(subscription => subscription.CreatedAt)
                        .Select(subscription => (TenantSubscriptionStatus?)subscription.Status).FirstOrDefault()
                    : null,
                ProvisioningStatus = jobs.Where(job => job.DatabaseResourceId == resource.Id)
                    .OrderByDescending(job => job.CreatedAt)
                    .Select(job => (ProvisioningJobStatus?)job.Status).FirstOrDefault(),
                ProvisioningError = jobs.Where(job => job.DatabaseResourceId == resource.Id)
                    .OrderByDescending(job => job.CreatedAt)
                    .Select(job => job.LastErrorCode).FirstOrDefault(),
                ReservedAtUtc = resource.ReservedAtUtc,
                AssignedAtUtc = resource.AssignedAtUtc,
                LastHealthCheckAtUtc = resource.LastHealthCheckAtUtc,
                LastConnectionTestAtUtc = resource.LastConnectionTestAtUtc,
                LastConnectionTestSucceeded = resource.LastConnectionTestSucceeded,
                LastConnectionTestDurationMs = resource.LastConnectionTestDurationMs,
                LastConnectionErrorCode = resource.LastConnectionErrorCode,
                LastConnectionErrorMessage = resource.LastConnectionErrorMessage,
                LastError = resource.LastError,
                SizeBytes = resource.SizeBytes,
                SchemaVersion = resource.SchemaVersion,
                BackupCount = backups.Count(backup => backup.DatabaseResourceId == resource.Id),
                LastBackupStatus = backups.Where(backup => backup.DatabaseResourceId == resource.Id)
                    .OrderByDescending(backup => backup.CreatedAt)
                    .Select(backup => (DatabaseBackupStatus?)backup.Status).FirstOrDefault(),
                LastBackupCompletedAtUtc = backups.Where(backup => backup.DatabaseResourceId == resource.Id)
                    .OrderByDescending(backup => backup.CreatedAt)
                    .Select(backup => backup.CompletedAtUtc).FirstOrDefault(),
                CanDelete = resource.ReservedForTenantId == null &&
                    resource.Status != DatabaseResourceStatus.Reserved && resource.Status != DatabaseResourceStatus.Provisioning &&
                    resource.Status != DatabaseResourceStatus.Assigned &&
                    !mappings.Any(mapping => mapping.DatabaseResourceId == resource.Id && mapping.IsActive) &&
                    !jobs.Any(job => job.DatabaseResourceId == resource.Id &&
                        (job.Status == ProvisioningJobStatus.Pending || job.Status == ProvisioningJobStatus.AwaitingDatabaseCapacity || job.Status == ProvisioningJobStatus.Provisioning)) &&
                    !restores.Any(restore => restore.TargetDatabaseResourceId == resource.Id &&
                        (restore.Status == RestoreJobStatus.Pending || restore.Status == RestoreJobStatus.Running)) &&
                    !backups.Any(backup => backup.DatabaseResourceId == resource.Id),
                DeletionBlockedReason = resource.ReservedForTenantId != null ||
                    resource.Status == DatabaseResourceStatus.Reserved || resource.Status == DatabaseResourceStatus.Provisioning || resource.Status == DatabaseResourceStatus.Assigned
                    ? "DATABASE_RESOURCE_RESERVED"
                    : mappings.Any(mapping => mapping.DatabaseResourceId == resource.Id && mapping.IsActive)
                        ? "DATABASE_RESOURCE_ASSIGNED"
                        : jobs.Any(job => job.DatabaseResourceId == resource.Id &&
                            (job.Status == ProvisioningJobStatus.Pending || job.Status == ProvisioningJobStatus.AwaitingDatabaseCapacity || job.Status == ProvisioningJobStatus.Provisioning))
                            ? "DATABASE_RESOURCE_PROVISIONING"
                            : restores.Any(restore => restore.TargetDatabaseResourceId == resource.Id &&
                                (restore.Status == RestoreJobStatus.Pending || restore.Status == RestoreJobStatus.Running))
                                ? "DATABASE_RESOURCE_RESTORE_ACTIVE"
                                : backups.Any(backup => backup.DatabaseResourceId == resource.Id)
                                    ? "DATABASE_RESOURCE_HAS_BACKUPS" : null,
                CreatedAtUtc = resource.CreatedAt,
                UpdatedAtUtc = resource.UpdatedAt
            });
    }

    private async Task<PlatformDatabaseResourceDto?> ToDtoAsync(Guid id, CancellationToken cancellationToken)
        => await Project(context.DatabaseResources.Where(resource => resource.Id == id))
            .SingleOrDefaultAsync(cancellationToken);

    private static void ApplyProbe(DatabaseResource resource, ConnectionProbe probe)
    {
        resource.LastConnectionTestAtUtc = probe.TestedAtUtc;
        resource.LastConnectionTestSucceeded = probe.Succeeded;
        resource.LastConnectionTestDurationMs = probe.DurationMs;
        resource.LastConnectionErrorCode = probe.ErrorCode;
        resource.LastConnectionErrorMessage = probe.Succeeded ? null : probe.Message;
        if (probe.Succeeded)
            resource.LastHealthCheckAtUtc = probe.TestedAtUtc;
    }

    private static PlatformConnectionTestDto ToConnectionTestDto(DatabaseResource resource, ConnectionProbe probe)
        => new(probe.Succeeded, resource.DatabaseName, resource.ServerHost, resource.ServerPort,
            probe.ActualDatabaseName, probe.ErrorCode, probe.Message, probe.DurationMs, probe.TestedAtUtc);

    private async Task<ConnectionProbe> ProbeAsync(string databaseName, string connectionString, CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();
        var testedAt = clock.UtcNow;
        try
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            if (string.IsNullOrWhiteSpace(builder.DataSource))
                return Failure(databaseName, "DATABASE_CONNECTION_SERVER_REQUIRED", "لم يتم تحديد خادم قاعدة البيانات.", started, testedAt);
            if (string.IsNullOrWhiteSpace(builder.InitialCatalog))
                builder.InitialCatalog = databaseName;
            builder.ConnectTimeout = Math.Clamp(builder.ConnectTimeout <= 0 ? 15 : builder.ConnectTimeout, 1, 15);

            await using var connection = new SqlConnection(builder.ConnectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT DB_NAME()";
            command.CommandTimeout = Math.Min(builder.ConnectTimeout, 15);
            var actualDatabaseName = Convert.ToString(await command.ExecuteScalarAsync(cancellationToken));
            return new ConnectionProbe(true, databaseName, actualDatabaseName, null, "تم الاتصال بقاعدة البيانات وفحصها بنجاح.",
                ElapsedMilliseconds(started), clock.UtcNow);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (ArgumentException)
        {
            return Failure(databaseName, "DATABASE_CONNECTION_STRING_INVALID", "صيغة سلسلة الاتصال غير صحيحة.", started, testedAt);
        }
        catch (Exception exception) when (exception is SqlException or TimeoutException or InvalidOperationException)
        {
            var errorCode = ClassifyConnectionFailure(exception);
            return Failure(databaseName, errorCode, ConnectionFailureMessage(errorCode), started, testedAt);
        }
    }

    private static ConnectionProbe Failure(string databaseName, string errorCode, string message, long started, DateTime testedAt)
        => new(false, databaseName, null, errorCode, message, ElapsedMilliseconds(started), testedAt);

    private static int ElapsedMilliseconds(long started)
        => (int)Math.Clamp(Stopwatch.GetElapsedTime(started).TotalMilliseconds, 0, int.MaxValue);

    private static string ClassifyConnectionFailure(Exception exception)
    {
        if (exception is SqlException sqlException)
        {
            if (sqlException.Number == -2) return "DATABASE_CONNECTION_TIMEOUT";
            if (sqlException.Number == 18456) return "DATABASE_AUTHENTICATION_FAILED";
            if (sqlException.Number == 4060) return "DATABASE_NOT_FOUND";
            if (sqlException.Number is 53 or 40 or 10061 or 11001) return "DATABASE_CONNECTION_REFUSED";
            if (sqlException.Message.Contains("certificate", StringComparison.OrdinalIgnoreCase) ||
                sqlException.Message.Contains("SSL", StringComparison.OrdinalIgnoreCase) ||
                sqlException.Message.Contains("TLS", StringComparison.OrdinalIgnoreCase))
                return "DATABASE_TLS_FAILED";
        }
        return exception is TimeoutException ? "DATABASE_CONNECTION_TIMEOUT" : "DATABASE_CONNECTION_FAILED";
    }

    private static string ConnectionFailureMessage(string errorCode) => errorCode switch
    {
        "DATABASE_CONNECTION_TIMEOUT" => "انتهت مهلة الاتصال. تحقق من الخادم والمنفذ.",
        "DATABASE_AUTHENTICATION_FAILED" => "فشل التحقق من بيانات دخول قاعدة البيانات.",
        "DATABASE_NOT_FOUND" => "قاعدة البيانات المطلوبة غير موجودة أو غير متاحة.",
        "DATABASE_CONNECTION_REFUSED" => "تعذر الوصول إلى خادم قاعدة البيانات.",
        "DATABASE_TLS_FAILED" => "فشل الاتصال الآمن بقاعدة البيانات. تحقق من إعدادات الشهادة.",
        _ => "فشل الاتصال بقاعدة البيانات. استخدم اختبار الاتصال أو أصلح المورد."
    };

    private static bool TryReadServerMetadata(string connectionString, out string? host, out int? port, out string error)
    {
        host = null;
        port = null;
        try
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            var dataSource = builder.DataSource?.Trim();
            if (string.IsNullOrWhiteSpace(dataSource))
            {
                error = "يجب أن تحتوي سلسلة الاتصال على عنوان خادم قاعدة البيانات.";
                return false;
            }

            dataSource = dataSource.TrimStart('@');
            foreach (var protocol in new[] { "tcp:", "np:", "lpc:" })
            {
                if (dataSource.StartsWith(protocol, StringComparison.OrdinalIgnoreCase))
                {
                    dataSource = dataSource[protocol.Length..];
                    break;
                }
            }

            var comma = dataSource.LastIndexOf(',');
            if (comma > 0 && int.TryParse(dataSource[(comma + 1)..], out var parsedPort))
            {
                port = parsedPort is > 0 and <= 65535 ? parsedPort : null;
                dataSource = dataSource[..comma];
            }
            host = dataSource.Length > 256 ? dataSource[..256] : dataSource;
            error = string.Empty;
            return true;
        }
        catch (ArgumentException)
        {
            error = "صيغة سلسلة الاتصال غير صحيحة.";
            return false;
        }
    }

    private sealed record ConnectionProbe(
        bool Succeeded,
        string DatabaseName,
        string? ActualDatabaseName,
        string? ErrorCode,
        string Message,
        int DurationMs,
        DateTime TestedAtUtc);
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
    public string ResourceCode { get; init; } = string.Empty;
    public string DatabaseName { get; init; } = string.Empty;
    public string Provider { get; init; } = string.Empty;
    public string? ServerKey { get; init; }
    public string? ServerHost { get; init; }
    public int? ServerPort { get; init; }
    public bool HasProtectedConnection { get; init; }
    public DatabaseResourceStatus Status { get; init; }
    public string LifecycleStatus { get; init; } = string.Empty;
    public Guid? TenantId { get; init; }
    public string? TenantName { get; init; }
    public WorkspaceType? WorkspaceType { get; init; }
    public TenantStatus? WorkspaceStatus { get; init; }
    public TenantSubscriptionStatus? SubscriptionStatus { get; init; }
    public ProvisioningJobStatus? ProvisioningStatus { get; init; }
    public string? ProvisioningError { get; init; }
    public DateTime? ReservedAtUtc { get; init; }
    public DateTime? AssignedAtUtc { get; init; }
    public DateTime? LastHealthCheckAtUtc { get; init; }
    public DateTime? LastConnectionTestAtUtc { get; init; }
    public bool? LastConnectionTestSucceeded { get; init; }
    public int? LastConnectionTestDurationMs { get; init; }
    public string? LastConnectionErrorCode { get; init; }
    public string? LastConnectionErrorMessage { get; init; }
    public string? LastError { get; init; }
    public long? SizeBytes { get; init; }
    public string? SchemaVersion { get; init; }
    public int BackupCount { get; init; }
    public DatabaseBackupStatus? LastBackupStatus { get; init; }
    public DateTime? LastBackupCompletedAtUtc { get; init; }
    public bool CanDelete { get; init; }
    public string? DeletionBlockedReason { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? UpdatedAtUtc { get; init; }
}

public sealed class TestConnectionRequest
{
    public string DatabaseName { get; init; } = string.Empty;
    public string ConnectionString { get; init; } = string.Empty;
}

public sealed class RepairConnectionRequest
{
    public string ConnectionString { get; init; } = string.Empty;
    public bool Confirm { get; init; }
}

public sealed class SetResourceStatusRequest
{
    public string Status { get; init; } = string.Empty;
}

public sealed record PlatformConnectionTestDto(
    bool Succeeded,
    string DatabaseName,
    string? ServerHost,
    int? ServerPort,
    string? ActualDatabaseName,
    string? ErrorCode,
    string Message,
    int? DurationMs,
    DateTime TestedAtUtc);

public sealed record PlatformResourceOperationDto(
    bool Succeeded,
    string Message,
    string? ErrorCode,
    string? SchemaVersion);
