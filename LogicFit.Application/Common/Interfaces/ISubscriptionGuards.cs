namespace LogicFit.Application.Common.Interfaces;

/// <summary>Known quota resource keys enforced against plan limits.</summary>
public static class QuotaResources
{
    public const string Members = "Members";
    public const string Coaches = "Coaches";
    public const string Branches = "Branches";
    public const string Employees = "Employees";
}

/// <summary>A command that requires a plan feature to be enabled for the current tenant.</summary>
public interface IRequireFeature
{
    string RequiredFeatureCode { get; }
}

/// <summary>A command that consumes a quota resource (checked against the plan limit before it runs).</summary>
public interface IRequireQuota
{
    string QuotaResource { get; }
}

/// <summary>Enforces plan feature-gates and usage limits for the current tenant.</summary>
public interface ITenantSubscriptionGuard
{
    Task EnsureFeatureAsync(string featureCode, CancellationToken cancellationToken = default);
    Task EnsureQuotaAsync(string quotaResource, CancellationToken cancellationToken = default);
}
