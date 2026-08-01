using LogicFit.Infrastructure.Persistence;
using Xunit;

namespace LogicFit.Tests;

public sealed class StartupDatabaseMigrationTests
{
    private static readonly string RepositoryRoot = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    [Fact]
    public void Startup_migrations_are_enabled_by_default_with_bounded_timeouts()
    {
        var options = new StartupDatabaseMigrationOptions();

        Assert.True(options.Enabled);
        Assert.True(StartupDatabaseMigrationOptions.IsValid(options));
        Assert.InRange(options.LockTimeoutSeconds, 1, 600);
        Assert.InRange(options.CommandTimeoutSeconds, 30, 1800);
    }

    [Theory]
    [InlineData(0, 300)]
    [InlineData(601, 300)]
    [InlineData(120, 29)]
    [InlineData(120, 1801)]
    public void Unsafe_timeout_configuration_is_rejected(int lockTimeout, int commandTimeout)
    {
        var options = new StartupDatabaseMigrationOptions
        {
            LockTimeoutSeconds = lockTimeout,
            CommandTimeoutSeconds = commandTimeout
        };

        Assert.False(StartupDatabaseMigrationOptions.IsValid(options));
    }

    [Fact]
    public void Startup_runs_migrations_before_data_seeding()
    {
        var program = File.ReadAllText(Path.Combine(RepositoryRoot, "LogicFit.API", "Program.cs"));

        var migrationIndex = program.IndexOf("ApplyPendingMigrationsAsync", StringComparison.Ordinal);
        var seedIndex = program.IndexOf("SeedAsync", StringComparison.Ordinal);

        Assert.True(migrationIndex >= 0, "Startup must invoke the database migrator.");
        Assert.True(seedIndex > migrationIndex, "Pending migrations must complete before data seeding.");
        Assert.DoesNotContain("Database__ApplyMigrationsOnStartup is ignored", program);
    }

    [Fact]
    public void Sql_server_migration_execution_is_serialized_and_verified()
    {
        var source = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "LogicFit.Infrastructure",
            "Persistence",
            "StartupDatabaseMigrator.cs"));

        Assert.Contains("sp_getapplock", source);
        Assert.Contains("sp_releaseapplock", source);
        Assert.Contains("GetPendingMigrationsAsync", source);
        Assert.Contains("MigrateAsync", source);
        Assert.Contains("remainingMigrations", source);
    }
}
