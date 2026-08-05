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
        Assert.Contains("LOGICFIT_JWT_SECRET", workflow);
        Assert.Contains("LOGICFIT_PASSWORD_RESET_SECRET", workflow);
        Assert.Contains("$xmlStart = $profilePayload.IndexOf('<')", workflow);
        Assert.Contains("$profileDocument = [xml]$profilePayload", workflow);
        Assert.Contains("[Convert]::FromBase64String($profilePayload)", workflow);

        Assert.Contains("does not match expected site", recovery);
        Assert.DoesNotContain("Temporary fixed OTP expiry", recovery);
        Assert.Contains("Recovery failed; restoring", recovery);
        Assert.Contains("Set-RemoteFile $remoteConfig $remoteConfigPath", recovery);
        Assert.Contains("Set-RemoteFile $remoteWebConfig $remoteWebConfigPath", recovery);
        Assert.Contains("without changing the database or application binary", recovery);
    }

    [Fact]
    public void Protected_deploy_accepts_base64_or_direct_publish_settings_xml()
    {
        var workflow = File.ReadAllText(Path.Combine(RepositoryRoot, ".github", "workflows", "cd.yml"));

        Assert.Contains("$profilePath = \"$env:RUNNER_TEMP/unified.publishSettings\"", workflow);
        Assert.Contains("$xmlStart = $profilePayload.IndexOf('<')", workflow);
        Assert.Contains("$profileDocument = [xml]$profilePayload", workflow);
        Assert.Contains("[Convert]::FromBase64String($profilePayload)", workflow);
        Assert.Contains(
            "The protected unified publish profile is neither Base64 nor publish-settings XML.",
            workflow);
    }

    [Fact]
    public void Read_only_webdeploy_diagnostic_never_syncs_to_the_remote_site()
    {
        var script = File.ReadAllText(Path.Combine(RepositoryRoot, "Scripts", "diagnose-webdeploy-health.ps1"));

        Assert.Contains("Get-RemoteFile", script);
        Assert.DoesNotContain("Set-RemoteFile", script);
        Assert.Contains("Remote server-configuration database connectivity probe", script);
        Assert.Contains("stdoutLogEnabled", script);
        Assert.Contains("Get-RemoteFile $remoteWebConfigPath", script);
        Assert.Contains("SELECT 1", File.ReadAllText(Path.Combine(RepositoryRoot, ".github", "workflows", "cd.yml")));
        Assert.Contains("without printing secrets", File.ReadAllText(Path.Combine(RepositoryRoot, ".github", "workflows", "cd.yml")));
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
