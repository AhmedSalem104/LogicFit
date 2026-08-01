namespace LogicFit.Infrastructure.Persistence;

public sealed class StartupDatabaseMigrationOptions
{
    public const string SectionName = "Database:StartupMigrations";

    /// <summary>
    /// Applies pending, compiled EF Core migrations before seeding and before the API starts
    /// accepting requests. Enabled by default so a normal publish cannot run an older schema.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Maximum time to wait for another API worker or instance to finish migrating the database.
    /// </summary>
    public int LockTimeoutSeconds { get; set; } = 120;

    /// <summary>
    /// Command timeout used while inspecting and applying migrations.
    /// </summary>
    public int CommandTimeoutSeconds { get; set; } = 300;

    public static bool IsValid(StartupDatabaseMigrationOptions options)
        => options.LockTimeoutSeconds is >= 1 and <= 600
           && options.CommandTimeoutSeconds is >= 30 and <= 1800;
}
