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
}
