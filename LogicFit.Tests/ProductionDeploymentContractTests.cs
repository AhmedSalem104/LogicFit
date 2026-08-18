using Xunit;
using LogicFit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Tests;

public class ProductionDeploymentContractTests
{
    private static readonly string RepositoryRoot = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    [Fact]
    public void Production_cd_requires_backup_review_and_protected_database_connection()
    {
        var workflow = File.ReadAllText(Path.Combine(RepositoryRoot, ".github", "workflows", "cd.yml"));

        Assert.Contains("backup_reference:", workflow);
        Assert.Contains("MIGRATIONS-REVIEWED", workflow);
        Assert.Contains("LOGICFIT_PRODUCTION_DB_CONNECTION", workflow);
        Assert.Contains("LOGICFIT_TEST_CONNECTION_STRING", workflow);
        Assert.Contains("mcr.microsoft.com/mssql/server:2022-latest", workflow);
        Assert.Contains("logicfit-production-migration-topology", workflow);
        Assert.Contains("Validate-EfMigrationTopology.ps1", workflow);
        Assert.Contains("application-compatibility.sql", workflow);
        var migrationGate = File.ReadAllText(Path.Combine(RepositoryRoot, "Scripts", "Validate-EfMigrationTopology.ps1"));
        Assert.Contains("platform.sql", migrationGate);
        Assert.Contains("tenant.sql", migrationGate);
        Assert.Contains("tree-equivalent to origin/master", workflow);
        Assert.Contains("needs.preflight.outputs.release_sha", workflow);
        Assert.Contains("DIAGNOSE-PRODUCTION-HEALTH", workflow);
        Assert.Contains("diagnose-production-health:", workflow);
        Assert.Contains("SELECT 1", workflow);
        Assert.Contains("shell: powershell", workflow);
        Assert.Contains("Microsoft.Data.SqlClient", File.ReadAllText(Path.Combine(RepositoryRoot, "LogicFit.Tests", "ProductionDatabaseConnectivityTests.cs")));
        Assert.Contains("Probe with the application SQL provider", workflow);
        Assert.Contains("diagnose-webdeploy-health.ps1", workflow);
        Assert.Contains("Compare remote database identity", workflow);
        Assert.Contains("Assert-NoTrackedSecrets.ps1", workflow);
        Assert.Contains("Block high and critical NuGet vulnerabilities", workflow);
        Assert.Contains("authenticated-e2e:", workflow);
        Assert.Contains("needs: [preflight, authenticated-e2e]", workflow);
        Assert.Contains("LOGICFIT_E2E_PLATFORM_TOKEN", workflow);
        Assert.Contains("Invoke-AuthenticatedReleaseE2E.ps1 -Mode Release", workflow);
    }

    [Fact]
    public void Webdeploy_applies_verified_migrations_before_sync_and_health_check()
    {
        var script = File.ReadAllText(Path.Combine(RepositoryRoot, "Scripts", "deploy-webdeploy.ps1"));

        var migrationIndex = script.IndexOf("dotnet ef database update", StringComparison.Ordinal);
        var webDeployIndex = script.IndexOf("& $MsDeployPath @arguments", StringComparison.Ordinal);
        var healthIndex = script.IndexOf("Invoke-WebRequest -Uri $HealthCheckUrl", StringComparison.Ordinal);

        Assert.True(migrationIndex >= 0, "The deployment helper must apply EF migrations.");
        Assert.True(webDeployIndex > migrationIndex, "Migrations must complete before WebDeploy sync.");
        Assert.True(healthIndex > webDeployIndex, "Health verification must run after WebDeploy sync.");
        Assert.Contains("VerifiedBackupReference", script);
        Assert.Contains("-ApplyMigrations requires -MigrationScriptPath", script);
        Assert.Contains("ApproveDestructiveMigrationReview", script);
        Assert.DoesNotContain("Database__ApplyMigrationsOnStartup", script);
    }

    [Fact]
    public void Ef_factory_uses_only_the_explicit_operator_connection_override()
    {
        const string variable = "LOGICFIT_EF_CONNECTION_STRING";
        const string operatorConnection = "Server=operator.example;Database=LogicFitOperator;User Id=operator;Password=test-only;TrustServerCertificate=True;";
        var previous = Environment.GetEnvironmentVariable(variable);

        try
        {
            Environment.SetEnvironmentVariable(variable, operatorConnection);
            using var context = new ApplicationDbContextFactory().CreateDbContext([]);

            Assert.Equal(operatorConnection, context.Database.GetConnectionString());
        }
        finally
        {
            Environment.SetEnvironmentVariable(variable, previous);
        }
    }

    [Fact]
    public void Protected_startup_recovery_can_explicitly_enable_private_backups()
    {
        var script = File.ReadAllText(Path.Combine(RepositoryRoot, "Scripts", "recover-webdeploy-startup.ps1"));
        var workflow = File.ReadAllText(Path.Combine(RepositoryRoot, ".github", "workflows", "cd.yml"));

        Assert.Contains("[switch] $EnableBackups", script, StringComparison.Ordinal);
        Assert.Contains("App_Data/PrivateBackups", script, StringComparison.Ordinal);
        Assert.Contains("enable_backups", workflow, StringComparison.Ordinal);
        Assert.Contains("$arguments.EnableBackups = $true", workflow, StringComparison.Ordinal);
    }

    [Fact]
    public void Repository_has_a_non_logging_secret_scan_release_gate()
    {
        var script = File.ReadAllText(Path.Combine(RepositoryRoot, "Scripts", "Assert-NoTrackedSecrets.ps1"));

        Assert.Contains("git -C $root ls-files", script, StringComparison.Ordinal);
        Assert.Contains("Write-Host", script, StringComparison.Ordinal);
        Assert.DoesNotContain("Write-Output $content", script, StringComparison.Ordinal);
        Assert.Contains("appsettings\\.production\\.json", script, StringComparison.OrdinalIgnoreCase);
    }
}
