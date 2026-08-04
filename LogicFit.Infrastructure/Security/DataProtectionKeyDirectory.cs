using Microsoft.Extensions.Configuration;

namespace LogicFit.Infrastructure.Security;

/// <summary>
/// Resolves the Data Protection key directory once and consistently. Relative paths are rooted
/// at the deployed application directory, never at the process working directory.
/// </summary>
public static class DataProtectionKeyDirectory
{
    public const string ConfigurationKey = "DataProtection:KeyDirectory";

    public static string Resolve(IConfiguration configuration)
    {
        var configured = configuration[ConfigurationKey];
        var path = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine("App_Data", "DataProtection-Keys")
            : configured.Trim();

        return Path.GetFullPath(
            Path.IsPathRooted(path)
                ? path
                : Path.Combine(AppContext.BaseDirectory, path));
    }
}
