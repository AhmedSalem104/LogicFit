using Xunit;

namespace LogicFit.Tests;

public sealed class PostDeploySmokeContractTests
{
    [Fact]
    public void Smoke_script_is_protected_and_covers_the_release_contract()
    {
        var source = File.ReadAllText(Path.Combine(RepositoryRoot, "Scripts", "post-deploy-smoke.ps1"));

        foreach (var required in new[]
        {
            "POST-DEPLOY-SMOKE-APPROVED",
            "AllowMutations",
            "VerifiedBackupReference",
            "ExpectedReleaseCommit",
            "LOGICFIT_SMOKE_PLATFORM_PASSWORD",
            "LOGICFIT_SMOKE_ALLOCATED_CONNECTION",
            "LOGICFIT_SMOKE_FAILED_CONNECTION",
            "/health",
            "/api/platform/diagnostics/version",
            "/api/platform/database-resources/",
            "/repair-connection",
            "/api/platform/tenants",
            "/api/identity/login",
            "/api/identity/select-workspace",
            "/api/tenant/my-subscription",
            "/api/tenant/plans",
            "/api/tenant/payment-requests",
            "/api/Notifications/unread-count",
            "/api/platform/subscriptions",
            "/api/platform/plans",
            "/api/platform/payment-requests",
            "/api/platform/notifications",
            "IDEMPOTENCY_KEY_INVALID",
            "Auditable smoke result"
        })
        {
            Assert.Contains(required, source, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Smoke_script_does_not_include_destructive_or_secret_output_paths()
    {
        var source = File.ReadAllText(Path.Combine(RepositoryRoot, "Scripts", "post-deploy-smoke.ps1"));

        Assert.DoesNotContain("Remove-Item", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DROP ", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("TRUNCATE", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/permanent-delete", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Write-Host $platformPassword", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Write-Host $allocatedConnection", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Write-Host $failedConnection", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Write-Host $script:PlatformToken", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Write-Host $script:TenantToken", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Smoke_runbook_requires_backup_and_operator_gates()
    {
        var documentation = File.ReadAllText(Path.Combine(RepositoryRoot, "docs", "POST-DEPLOY-SMOKE.md"));

        Assert.Contains("verified backup", documentation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("-AllowMutations", documentation, StringComparison.Ordinal);
        Assert.Contains("POST-DEPLOY-SMOKE-APPROVED", documentation, StringComparison.Ordinal);
        Assert.Contains("non-destructive", documentation, StringComparison.OrdinalIgnoreCase);
    }

    private static string RepositoryRoot => Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
}
