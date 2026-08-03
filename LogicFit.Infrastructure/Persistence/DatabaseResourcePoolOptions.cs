namespace LogicFit.Infrastructure.Persistence;

/// <summary>
/// Server-only configuration for operator-created tenant database resources. Connection strings
/// must be supplied by deployment secrets/environment variables and are encrypted before they are
/// persisted in the Platform database.
/// </summary>
public sealed class DatabaseResourcePoolOptions
{
    public const string SectionName = "DatabaseResourcePool";

    /// <summary>
    /// Registers configured resources during startup. Existing Assigned/Reserved resources are
    /// never reset or replaced by this operation.
    /// </summary>
    public bool SeedConfiguredResources { get; set; } = true;

    public List<DatabaseResourceDefinition> Resources { get; set; } = new();

    public static bool IsValid(DatabaseResourcePoolOptions options)
    {
        if (!options.SeedConfiguredResources)
            return true;

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var resource in options.Resources)
        {
            if (string.IsNullOrWhiteSpace(resource.DatabaseName) ||
                string.IsNullOrWhiteSpace(resource.ConnectionString))
                return false;

            var key = $"{resource.Provider}:{resource.DatabaseName}";
            if (!seen.Add(key))
                return false;
        }

        return true;
    }
}

public sealed class DatabaseResourceDefinition
{
    public string Provider { get; set; } = "ManualMonster";
    public string DatabaseName { get; set; } = string.Empty;
    public string? ServerKey { get; set; }
    public string ConnectionString { get; set; } = string.Empty;
}
