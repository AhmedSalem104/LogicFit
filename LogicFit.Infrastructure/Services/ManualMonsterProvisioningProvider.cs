using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Authorization;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LogicFit.Infrastructure.Services;

/// <summary>
/// Provisions an operator-registered Monster database. It never creates or deletes a Monster
/// database; it reserves a pool row, applies the tenant migration assembly, seeds the owner, runs a
/// health check, and records the encrypted mapping. The workflow is retry-safe by tenant and
/// resource identifiers and never logs connection material.
/// </summary>
public sealed class ManualMonsterProvisioningProvider : IDatabaseProvisioningProvider
{
    private readonly IDatabaseResourcePool resourcePool;
    private readonly PlatformDbContext? platformDb;
    private readonly ApplicationDbContext? legacyDb;
    private readonly TenantDatabaseSeeder? tenantDatabaseSeeder;
    private readonly IConnectionStringProtector? connectionStringProtector;
    private readonly IDateTimeService? dateTime;
    private readonly ILogger<ManualMonsterProvisioningProvider>? logger;

    // Lightweight capacity-only constructor retained for provider contract tests.
    public ManualMonsterProvisioningProvider(IDatabaseResourcePool resourcePool)
        => this.resourcePool = resourcePool;

    [ActivatorUtilitiesConstructor]
    public ManualMonsterProvisioningProvider(
        IDatabaseResourcePool resourcePool,
        PlatformDbContext platformDb,
        ApplicationDbContext legacyDb,
        TenantDatabaseSeeder tenantDatabaseSeeder,
        IConnectionStringProtector connectionStringProtector,
        IDateTimeService dateTime,
        ILogger<ManualMonsterProvisioningProvider> logger)
    {
        this.resourcePool = resourcePool;
        this.platformDb = platformDb;
        this.legacyDb = legacyDb;
        this.tenantDatabaseSeeder = tenantDatabaseSeeder;
        this.connectionStringProtector = connectionStringProtector;
        this.dateTime = dateTime;
        this.logger = logger;
    }

    public string ProviderName => "ManualMonster";

    public async Task<DatabaseProvisioningResult> ProvisionAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        // The one-argument constructor is intentionally usable by capacity-only contract tests,
        // but a real provisioning run must have all platform services available.  Check this
        // after reserving so a no-capacity environment still reports its actionable state.
        if (platformDb is null || legacyDb is null || tenantDatabaseSeeder is null || connectionStringProtector is null || dateTime is null)
        {
            var unconfiguredReservation = await resourcePool.ReserveAvailableAsync(tenantId, cancellationToken);
            if (unconfiguredReservation is null)
                return new DatabaseProvisioningResult(
                    "AwaitingDatabaseCapacity",
                    tenantId,
                    null,
                    ProviderName,
                    null,
                    "DATABASE_CAPACITY_UNAVAILABLE");

            await resourcePool.ReleaseAsync(unconfiguredReservation.ResourceId, tenantId, cancellationToken);
            return new DatabaseProvisioningResult(
                "ProvisioningFailed",
                tenantId,
                unconfiguredReservation.ResourceId,
                ProviderName,
                unconfiguredReservation.DatabaseName,
                "PROVIDER_NOT_CONFIGURED");
        }

        var existingMapping = await platformDb.TenantDatabaseMappings
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.IsActive, cancellationToken);
        if (existingMapping is not null)
        {
            var mappedResource = await platformDb.DatabaseResources
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == existingMapping.DatabaseResourceId && x.Status == DatabaseResourceStatus.Assigned, cancellationToken);
            if (mappedResource is null || string.IsNullOrWhiteSpace(mappedResource.EncryptedConnectionString))
                return Failed(tenantId, existingMapping.DatabaseResourceId, "DATABASE_MAPPING_INVALID");

            var mappedConnection = connectionStringProtector.Unprotect(mappedResource.EncryptedConnectionString);
            var mappedOptions = new DbContextOptionsBuilder<TenantDbContext>();
            DbContextSqlServerOptions.UseTenantDatabase(mappedOptions, mappedConnection);
            await using var mappedTenantDb = new TenantDbContext(mappedOptions.Options, tenantId);
            var ownerIdentityId = await platformDb.ApplicationRequests
                .Where(x => x.ProvisionedWorkspaceId == tenantId)
                .Select(x => (Guid?)x.IdentityAccountId)
                .FirstOrDefaultAsync(cancellationToken);
            if (!ownerIdentityId.HasValue)
                return Failed(tenantId, existingMapping.DatabaseResourceId, "APPLICATION_OWNER_NOT_FOUND");
            var existingOwner = await mappedTenantDb.Users.IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.IsActive && !x.IsDeleted && x.IdentityAccountId == ownerIdentityId.Value, cancellationToken);
            return new DatabaseProvisioningResult(
                "Completed",
                tenantId,
                existingMapping.DatabaseResourceId,
                existingMapping.Provider,
                null,
                null,
                existingOwner?.Id,
                existingMapping.SchemaVersion);
        }

        var reservation = await resourcePool.ReserveAvailableAsync(tenantId, cancellationToken);
        if (reservation is null)
            return new DatabaseProvisioningResult(
                "AwaitingDatabaseCapacity",
                tenantId,
                null,
                ProviderName,
                null,
                "DATABASE_CAPACITY_UNAVAILABLE");

        var resource = await platformDb.DatabaseResources
            .FirstOrDefaultAsync(x => x.Id == reservation.ResourceId && x.ReservedForTenantId == tenantId, cancellationToken);
        if (resource is null)
        {
            await resourcePool.ReleaseAsync(reservation.ResourceId, tenantId, cancellationToken);
            return Failed(tenantId, reservation.ResourceId, "DATABASE_RESOURCE_NOT_FOUND");
        }

        resource.Status = DatabaseResourceStatus.Provisioning;
        await platformDb.SaveChangesAsync(cancellationToken);

        try
        {
            if (string.IsNullOrWhiteSpace(resource.EncryptedConnectionString))
                return await FailResourceAsync(resource, tenantId, reservation.ResourceId, "DATABASE_CONNECTION_NOT_CONFIGURED", cancellationToken);
            var connectionString = connectionStringProtector.Unprotect(resource.EncryptedConnectionString);
            var options = new DbContextOptionsBuilder<TenantDbContext>();
            DbContextSqlServerOptions.UseTenantDatabase(options, connectionString);
            await using var tenantDb = new TenantDbContext(options.Options, tenantId);

            // The Tenant migrations assembly is independent from the Platform history table.
            await tenantDb.Database.MigrateAsync(cancellationToken);
            if (!await tenantDb.Database.CanConnectAsync(cancellationToken))
                return await FailResourceAsync(resource, tenantId, reservation.ResourceId, "TENANT_DATABASE_HEALTH_CHECK_FAILED", cancellationToken);

            await tenantDatabaseSeeder.SeedAsync(tenantDb, cancellationToken);

            var application = await platformDb.ApplicationRequests
                .Include(x => x.IdentityAccount)
                .FirstOrDefaultAsync(x => x.ProvisionedWorkspaceId == tenantId, cancellationToken);
            if (application?.IdentityAccount is null)
                return await FailResourceAsync(resource, tenantId, reservation.ResourceId, "APPLICATION_OWNER_NOT_FOUND", cancellationToken);

            var localOwner = await tenantDb.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.IdentityAccountId == application.IdentityAccountId && !x.IsDeleted, cancellationToken);
            // Platform-created gyms already have a compatibility owner row. Reusing that id in
            // the tenant database keeps the membership foreign key stable across the split. A
            // freelance workspace has no central owner yet, so it receives a new local id.
            var existingPlatformOwnerId = await legacyDb.Set<User>()
                .IgnoreQueryFilters()
                .Where(x => x.TenantId == tenantId && x.IdentityAccountId == application.IdentityAccountId && !x.IsDeleted)
                .Select(x => (Guid?)x.Id)
                .FirstOrDefaultAsync(cancellationToken);
            var ownerRoleName = application.RequestedRole == UserRole.FreelanceOwner
                ? SystemRoles.FreelanceOwner
                : SystemRoles.Owner;
            var ownerPermissionCodes = Permissions.TenantPermissions.ToArray();
            var permissions = await tenantDb.Permissions
                .Where(x => ownerPermissionCodes.Contains(x.Code))
                .ToDictionaryAsync(x => x.Code, cancellationToken);
            foreach (var permissionCode in ownerPermissionCodes)
            {
                if (permissions.ContainsKey(permissionCode))
                    continue;

                var permission = new Permission
                {
                    Code = permissionCode,
                    DisplayName = permissionCode,
                    DisplayNameAr = permissionCode,
                    Category = "Tenant",
                    IsPlatformPermission = false
                };
                tenantDb.Permissions.Add(permission);
                permissions[permissionCode] = permission;
            }

            var ownerRole = await tenantDb.AppRoles.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.TenantId == null && x.Name == ownerRoleName && !x.IsDeleted, cancellationToken);
            if (ownerRole is null)
            {
                ownerRole = new Role
                {
                    Name = ownerRoleName,
                    NameAr = ownerRoleName,
                    NormalizedName = ownerRoleName.ToUpperInvariant(),
                    IsSystemRole = true
                };
                tenantDb.AppRoles.Add(ownerRole);
            }

            if (localOwner is null)
            {
                localOwner = new User
                {
                    Id = existingPlatformOwnerId ?? Guid.NewGuid(),
                    TenantId = tenantId,
                    IdentityAccountId = application.IdentityAccountId,
                    Email = application.IdentityAccount.Email,
                    PhoneNumber = application.IdentityAccount.PhoneNumber,
                    PasswordHash = application.IdentityAccount.PasswordHash,
                    Role = application.RequestedRole ?? UserRole.Owner,
                    IsActive = true
                };
                tenantDb.Users.Add(localOwner);
                tenantDb.UserProfiles.Add(new UserProfile
                {
                    UserId = localOwner.Id,
                    FullName = application.IdentityAccount.FullName,
                });
            }

            await tenantDb.SaveChangesAsync(cancellationToken);
            if (!await tenantDb.UserRoleAssignments
                    .IgnoreQueryFilters()
                    .AnyAsync(x => x.UserId == localOwner.Id && x.RoleId == ownerRole.Id && x.TenantId == tenantId, cancellationToken))
            {
                tenantDb.UserRoleAssignments.Add(new UserRoleAssignment
                {
                    UserId = localOwner.Id,
                    Role = ownerRole,
                    RoleId = ownerRole.Id,
                    TenantId = tenantId
                });
                await tenantDb.SaveChangesAsync(cancellationToken);
            }

            var mappedPermissionIds = await tenantDb.RolePermissions
                .Where(x => x.RoleId == ownerRole.Id)
                .Select(x => x.PermissionId)
                .ToListAsync(cancellationToken);
            foreach (var permissionCode in ownerPermissionCodes)
            {
                var permissionId = permissions[permissionCode].Id;
                if (mappedPermissionIds.Contains(permissionId))
                    continue;
                tenantDb.RolePermissions.Add(new RolePermission
                {
                    RoleId = ownerRole.Id,
                    PermissionId = permissionId
                });
            }
            await tenantDb.SaveChangesAsync(cancellationToken);

            var mapping = await platformDb.TenantDatabaseMappings
                .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.IsActive, cancellationToken);
            if (mapping is null)
            {
                platformDb.TenantDatabaseMappings.Add(new TenantDatabaseMapping
                {
                    TenantId = tenantId,
                    DatabaseResourceId = resource.Id,
                    Provider = string.IsNullOrWhiteSpace(resource.Provider) ? ProviderName : resource.Provider,
                    EncryptedConnectionString = resource.EncryptedConnectionString,
                    SchemaVersion = TenantDbContext.MigrationsAssemblyName,
                    LastValidatedAtUtc = dateTime.UtcNow,
                    IsActive = true
                });
            }

            resource.Status = DatabaseResourceStatus.Assigned;
            resource.AssignedAtUtc = dateTime.UtcNow;
            resource.SchemaVersion = TenantDbContext.MigrationsAssemblyName;
            resource.LastHealthCheckAtUtc = dateTime.UtcNow;
            resource.LastError = null;
            var providerName = string.IsNullOrWhiteSpace(resource.Provider) ? ProviderName : resource.Provider;
            await platformDb.SaveChangesAsync(cancellationToken);
            return new DatabaseProvisioningResult(
                "Completed",
                tenantId,
                resource.Id,
                providerName,
                resource.DatabaseName,
                null,
                localOwner.Id,
                TenantDbContext.MigrationsAssemblyName);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger?.LogError(exception, "Tenant provisioning failed for TenantId {TenantId} and ResourceId {ResourceId}.", tenantId, resource.Id);
            return await FailResourceAsync(resource, tenantId, resource.Id, "TENANT_PROVISIONING_FAILED", cancellationToken);
        }
    }

    private DatabaseProvisioningResult Failed(Guid tenantId, Guid resourceId, string code)
        => new("ProvisioningFailed", tenantId, resourceId, ProviderName, null, code);

    private async Task<DatabaseProvisioningResult> FailResourceAsync(
        DatabaseResource resource,
        Guid tenantId,
        Guid resourceId,
        string errorCode,
        CancellationToken cancellationToken)
    {
        resource.Status = DatabaseResourceStatus.Faulted;
        resource.LastError = errorCode;
        await platformDb!.SaveChangesAsync(cancellationToken);
        return Failed(tenantId, resourceId, errorCode);
    }
}
