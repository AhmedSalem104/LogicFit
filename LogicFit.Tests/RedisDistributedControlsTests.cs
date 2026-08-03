using LogicFit.API.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace LogicFit.Tests;

public sealed class RedisDistributedControlsTests
{
    private static readonly string RepositoryRoot = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    [Fact]
    public void Nonproduction_without_redis_uses_the_explicit_local_defaults()
    {
        var configuration = new ConfigurationBuilder().Build();
        var environment = new TestHostEnvironment { EnvironmentName = Environments.Development };

        var settings = RedisConnectionSettings.TryResolve(configuration);

        Assert.Null(settings);
        Assert.False(RedisConnectionSettings.IsRequired(configuration, environment));
        Assert.False(RedisConnectionSettings.IsEnabled(configuration, settings));
        RedisConnectionSettings.Validate(required: false, enabled: false, settings);
    }

    [Fact]
    public void Production_without_redis_is_rejected_before_service_registration()
    {
        var configuration = new ConfigurationBuilder().Build();
        var environment = new TestHostEnvironment { EnvironmentName = Environments.Production };

        Assert.True(RedisConnectionSettings.IsRequired(configuration, environment));
        var exception = Assert.Throws<InvalidOperationException>(() =>
            RedisConnectionSettings.Validate(required: true, enabled: false, settings: null));

        Assert.Contains("Redis is required", exception.Message);
    }

    [Fact]
    public void Password_file_is_read_only_when_an_endpoint_is_configured()
    {
        var passwordFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(passwordFile, "redis-test-secret\r\n");
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Redis:Endpoint"] = "localhost:6379",
                    ["Redis:PasswordFile"] = passwordFile,
                    ["Redis:InstanceName"] = "LogicFitTest"
                })
                .Build();

            var settings = RedisConnectionSettings.TryResolve(configuration);

            Assert.NotNull(settings);
            Assert.Equal("LogicFitTest:", settings!.InstanceName);
            Assert.Equal("redis-test-secret", settings.CreateConfigurationOptions().Password);
        }
        finally
        {
            File.Delete(passwordFile);
        }
    }

    [Fact]
    public void Password_and_password_file_cannot_be_configured_together()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Redis:Endpoint"] = "localhost:6379",
                ["Redis:Password"] = "inline-secret",
                ["Redis:PasswordFile"] = "external-secret-file"
            })
            .Build();

        var exception = Assert.Throws<InvalidOperationException>(
            () => RedisConnectionSettings.TryResolve(configuration));

        Assert.Contains("only one", exception.Message);
    }

    [Fact]
    public void Redis_rate_limiter_uses_atomic_window_operations_and_gateway_delegation_is_explicit()
    {
        var program = File.ReadAllText(Path.Combine(RepositoryRoot, "LogicFit.API", "Program.cs"));
        var limiter = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "LogicFit.API",
            "RateLimiting",
            "RedisFixedWindowRateLimiter.cs"));

        Assert.Contains("AddStackExchangeRedisCache", program);
        Assert.Contains("RateLimiting:ManagedByGateway", program);
        Assert.Contains("RedisFixedWindowRateLimiter", program);
        Assert.Contains("INCRBY", limiter);
        Assert.Contains("PEXPIRE", limiter);
        Assert.Contains("return {-1, ttl}", limiter);
        Assert.Contains("limiter outage must not silently turn into an unlimited endpoint", limiter);
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;

        public string ApplicationName { get; set; } = nameof(RedisDistributedControlsTests);

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
