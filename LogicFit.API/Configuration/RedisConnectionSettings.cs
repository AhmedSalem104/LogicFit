using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

namespace LogicFit.API.Configuration;

/// <summary>
/// Resolves Redis connection material without placing credentials in source or logs.
/// A password can be supplied through a secret file when the endpoint is managed separately.
/// </summary>
public sealed class RedisConnectionSettings
{
    private readonly string? _connectionString;
    private readonly string? _endpoint;
    private readonly string? _password;

    private RedisConnectionSettings(
        string? connectionString,
        string? endpoint,
        string? password,
        string instanceName)
    {
        _connectionString = connectionString;
        _endpoint = endpoint;
        _password = password;
        InstanceName = instanceName;
    }

    public string InstanceName { get; }

    public static RedisConnectionSettings? TryResolve(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var instanceName = NormalizeInstanceName(configuration["Redis:InstanceName"]);
        var connectionString = configuration.GetConnectionString("Redis")
            ?? configuration["Redis:ConnectionString"];

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            // Parse now so a malformed secret fails during startup, before the first request.
            _ = ConfigurationOptions.Parse(connectionString);
            return new RedisConnectionSettings(connectionString.Trim(), null, null, instanceName);
        }

        var endpoint = configuration["Redis:Endpoint"];
        if (string.IsNullOrWhiteSpace(endpoint))
            return null;

        var password = configuration["Redis:Password"];
        var passwordFile = configuration["Redis:PasswordFile"];
        if (!string.IsNullOrWhiteSpace(password) && !string.IsNullOrWhiteSpace(passwordFile))
        {
            throw new InvalidOperationException(
                "Configure only one of Redis:Password and Redis:PasswordFile.");
        }

        if (!string.IsNullOrWhiteSpace(passwordFile))
        {
            var resolvedPath = ResolvePasswordFilePath(passwordFile);
            if (!File.Exists(resolvedPath))
            {
                throw new InvalidOperationException(
                    "Redis:PasswordFile does not point to an existing secret file.");
            }

            password = File.ReadAllText(resolvedPath).Trim();
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException(
                    "Redis:PasswordFile is empty.");
            }
        }

        var endpointOptions = ConfigurationOptions.Parse(endpoint.Trim());
        if (string.IsNullOrWhiteSpace(password) && !string.IsNullOrWhiteSpace(endpointOptions.Password))
            password = endpointOptions.Password;

        return new RedisConnectionSettings(null, endpoint.Trim(), password, instanceName);
    }

    public ConfigurationOptions CreateConfigurationOptions()
    {
        var options = ConfigurationOptions.Parse(_connectionString ?? _endpoint!);
        if (!string.IsNullOrWhiteSpace(_password))
            options.Password = _password;

        return options;
    }

    public static bool IsRequired(IConfiguration configuration, IHostEnvironment environment)
    {
        return configuration.GetValue<bool?>("Redis:Required") ?? environment.IsProduction();
    }

    public static bool IsEnabled(IConfiguration configuration, RedisConnectionSettings? settings)
    {
        return configuration.GetValue<bool?>("Redis:Enabled") ?? settings is not null;
    }

    public static void Validate(
        bool required,
        bool enabled,
        RedisConnectionSettings? settings)
    {
        if (enabled && settings is null)
        {
            throw new InvalidOperationException(
                "Redis is enabled but no Redis connection configuration was supplied.");
        }

        if (required && (!enabled || settings is null))
        {
            throw new InvalidOperationException(
                "Redis is required for this environment; configure a Redis connection before startup.");
        }
    }

    private static string NormalizeInstanceName(string? instanceName)
    {
        var value = string.IsNullOrWhiteSpace(instanceName) ? "LogicFit" : instanceName.Trim();
        return value.TrimEnd(':') + ":";
    }

    private static string ResolvePasswordFilePath(string path)
    {
        // Accept the /c/... spelling commonly copied from Git Bash on Windows while keeping
        // ordinary absolute and relative paths untouched.
        if (OperatingSystem.IsWindows()
            && path.Length >= 4
            && path[0] == '/'
            && char.IsLetter(path[1])
            && path[2] == '/')
        {
            return $"{char.ToUpperInvariant(path[1])}:\\{path[3..].Replace('/', '\\')}";
        }

        return path;
    }
}
