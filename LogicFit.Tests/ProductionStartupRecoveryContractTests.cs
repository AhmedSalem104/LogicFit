using Xunit;

namespace LogicFit.Tests;

public sealed class ProductionStartupRecoveryContractTests
{
    private static readonly string RepositoryRoot = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    [Fact]
    public void Publish_artifact_and_webdeploy_cannot_overwrite_production_configuration()
    {
        var project = File.ReadAllText(Path.Combine(RepositoryRoot, "LogicFit.API", "LogicFit.API.csproj"));
        var deploy = File.ReadAllText(Path.Combine(RepositoryRoot, "Scripts", "deploy-webdeploy.ps1"));

        Assert.Contains("appsettings.Production.json", project);
        Assert.Contains("CopyToPublishDirectory=\"Never\"", project);
        Assert.Contains("-skip:objectName=filePath,absolutePath=appsettings\\.Production\\.json$", deploy);
    }

    [Fact]
    public void Recovery_is_protected_target_bound_and_rolls_back_on_failed_health()
    {
        var workflow = File.ReadAllText(Path.Combine(RepositoryRoot, ".github", "workflows", "cd.yml"));
        var recovery = File.ReadAllText(Path.Combine(RepositoryRoot, "Scripts", "recover-webdeploy-startup.ps1"));

        Assert.Contains("RECOVER-PRODUCTION-STARTUP", workflow);
        Assert.Contains("recover-startup:", workflow);
        Assert.Contains("environment: production", workflow);
        Assert.Contains("LOGICFIT_OTP_HMAC_SECRET", workflow);
        Assert.Contains("LOGICFIT_JWT_SECRET", workflow);
        Assert.Contains("LOGICFIT_PASSWORD_RESET_SECRET", workflow);
        Assert.Contains("$profilePayload.StartsWith('<')", workflow);
        Assert.Contains("[Convert]::FromBase64String($profilePayload)", workflow);

        Assert.Contains("does not match expected site", recovery);
        Assert.Contains("Temporary fixed OTP expiry", recovery);
        Assert.Contains("Recovery failed; restoring", recovery);
        Assert.Contains("Set-RemoteFile $remoteConfig $remoteConfigPath", recovery);
        Assert.Contains("Set-RemoteFile $remoteWebConfig $remoteWebConfigPath", recovery);
        Assert.Contains("without changing the database or application binary", recovery);
    }

    [Fact]
    public void Authentication_request_payloads_are_never_written_by_exception_behavior()
    {
        var behavior = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "LogicFit.Application",
            "Common",
            "Behaviors",
            "UnhandledExceptionBehavior.cs"));

        Assert.Contains(
            "_logger.LogError(ex, \"Unhandled Exception for Request {RequestName}\", requestName);",
            behavior);
        Assert.DoesNotContain("requestName, request", behavior, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("{@Request}", behavior, StringComparison.OrdinalIgnoreCase);
    }
}
