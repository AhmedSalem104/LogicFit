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
        Assert.Contains("logicfit-production-migration-plan", workflow);
        Assert.Contains("tree-equivalent to origin/master", workflow);
        Assert.Contains("needs.preflight.outputs.release_sha", workflow);
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
        Assert.Contains("App_Data\\\\DataProtection-Keys", script);
        Assert.Contains("objectName=dirPath", script);
        Assert.DoesNotContain("Database__ApplyMigrationsOnStartup", script);
    }

    [Fact]
    public void Data_protection_keys_use_the_central_database_with_a_file_recovery_mirror()
    {
        var dependencyInjection = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "LogicFit.Infrastructure",
            "DependencyInjection.cs"));
        var bootstrapper = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "LogicFit.Infrastructure",
            "Persistence",
            "DataProtectionKeyRingBootstrapper.cs"));

        Assert.Contains("PersistKeysToDbContext<ApplicationDbContext>", dependencyInjection);
        Assert.Contains("DataProtectionKeyRingBootstrapper", bootstrapper);
        Assert.Contains("FileSystemXmlRepository", bootstrapper);
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
}
