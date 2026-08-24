using Xunit;

namespace LogicFit.Tests;

public sealed class ProductionRemediationContractTests
{
    private static readonly string RepositoryRoot = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    [Fact]
    public void Protected_mapping_health_diagnostics_identify_the_failing_row_without_logging_ciphertext()
    {
        var source = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "LogicFit.Infrastructure",
            "HealthChecks",
            "TenantDatabaseMappingHealthCheck.cs"));

        Assert.Contains("ProtectedValueId", source);
        Assert.Contains("DatabaseResourceId", source);
        Assert.Contains("TenantId", source);
        Assert.Contains("DatabaseName", source);
        Assert.DoesNotContain(
            "protectedValue.Value,",
            source,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Production_repair_endpoint_keeps_allocated_rows_on_the_wrench_path()
    {
        var source = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "LogicFit.API",
            "Features",
            "Platform",
            "DatabaseResources",
            "PlatformDatabaseResourcesController.cs"));

        Assert.Contains("repair-connection", source);
        Assert.Contains("DATABASE_RESOURCE_REPAIR_NOT_ALLOWED", source);
        Assert.Contains("DatabaseResourceConnectionRepaired", source);
        Assert.Contains("AffectedColumns = \"EncryptedConnectionString,LastValidatedAtUtc,LastHealthCheckAtUtc\"", source);
        Assert.Contains("resource.Status = DatabaseResourceStatus.Available", source);
    }

    [Fact]
    public void Readiness_checks_only_runtime_database_resources()
    {
        var source = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "LogicFit.Infrastructure",
            "HealthChecks",
            "TenantDatabaseMappingHealthCheck.cs"));

        Assert.Contains("DatabaseResourceStatus.Reserved", source);
        Assert.Contains("DatabaseResourceStatus.Provisioning", source);
        Assert.Contains("DatabaseResourceStatus.Assigned", source);
        Assert.Contains("faulted/retired/unallocated", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Backup_resolution_fails_closed_when_a_mapping_cannot_be_decrypted()
    {
        var source = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "LogicFit.Infrastructure",
            "Services",
            "DatabaseBackupService.cs"));

        Assert.Contains("CryptographicException", source);
        Assert.Contains("false complete coverage", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Repair the mapping before retrying", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Central_backup_uses_the_shared_sql_application_lock_provider()
    {
        var serviceSource = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "LogicFit.Infrastructure",
            "Services",
            "DatabaseBackupService.cs"));
        var providerSource = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "LogicFit.Infrastructure",
            "Services",
            "SqlServerDistributedLockProvider.cs"));

        Assert.Contains("IDistributedLockProvider", serviceSource, StringComparison.Ordinal);
        Assert.Contains("TryAcquireAsync", serviceSource, StringComparison.Ordinal);
        Assert.Contains("sys.sp_getapplock", providerSource, StringComparison.Ordinal);
        Assert.Contains("SELECT @result", providerSource, StringComparison.Ordinal);
        Assert.Contains("ExecuteScalarAsync", providerSource, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_data_protection_uses_the_platform_database_key_ring()
    {
        var infrastructureSource = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "LogicFit.Infrastructure",
            "DependencyInjection.cs"));
        var apiSource = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "LogicFit.API",
            "Program.cs"));

        Assert.Contains("SetApplicationName(\"LogicFit\")", infrastructureSource, StringComparison.Ordinal);
        Assert.Contains("PersistKeysToDbContext<ApplicationDbContext>()", infrastructureSource, StringComparison.Ordinal);
        Assert.Contains("AddScoped<DataProtectionKeyRingBootstrapper>()", infrastructureSource, StringComparison.Ordinal);
        Assert.Contains("AddScoped<DatabaseResourceSeeder>()", infrastructureSource, StringComparison.Ordinal);
        Assert.Contains("SynchronizeAsync", apiSource, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_schema_reconciliation_adds_only_the_missing_invite_index()
    {
        var migration = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "LogicFit.Infrastructure",
            "Persistence",
            "Migrations",
            "20260824130000_ReconcileProductionSchemaIndexes.cs"));

        Assert.Contains("IX_WorkspaceInvites_InvitedByMembershipId", migration, StringComparison.Ordinal);
        Assert.Contains("CREATE INDEX", migration, StringComparison.Ordinal);
        Assert.Contains("IF OBJECT_ID", migration, StringComparison.Ordinal);
        Assert.DoesNotContain("DROP INDEX IX_ApplicationRequests_TargetScopeKey_ApplicationType", migration, StringComparison.Ordinal);
    }

    [Fact]
    public void Startup_seeding_is_serialized_and_does_not_allow_destructive_food_reset_from_environment()
    {
        var seeder = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "LogicFit.Infrastructure",
            "Persistence",
            "DataSeeder.cs"));
        var program = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "LogicFit.API",
            "Program.cs"));

        Assert.Contains("IDistributedLockProvider", seeder, StringComparison.Ordinal);
        Assert.Contains("LogicFit:PlatformDataSeeder", seeder, StringComparison.Ordinal);
        Assert.Contains("TryAcquireAsync", seeder, StringComparison.Ordinal);
        Assert.Contains("another API worker", seeder, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("RESET_FOODS", program, StringComparison.Ordinal);
    }
}
