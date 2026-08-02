using LogicFit.Domain.Enums;

namespace LogicFit.Application.Common.Services;

/// <summary>The workspace and subscription state required for access control.</summary>
public sealed record TenantAccessState(
    bool TenantExists,
    TenantStatus TenantStatus,
    TenantSubscriptionStatus? SubscriptionStatus,
    SuspensionReason? SuspensionReason,
    WorkspaceType WorkspaceType = WorkspaceType.Gym);

/// <summary>A workspace-wide hard block with its stable machine-readable code.</summary>
public sealed record TenantBlock(string Code, int HttpStatus);

/// <summary>Effective workspace access after workspace and subscription rules are applied.</summary>
public enum TenantAccessMode
{
    Full,
    BillingOnly,
    ReadOnly,
    Blocked
}

public sealed record TenantAccessDecision(TenantAccessMode Mode, TenantBlock? Block = null);

/// <summary>
/// DB-free workspace access policy. Its precedence is fixed: workspace status, membership status
/// (enforced by authorization), subscription status, then role/permission checks.
/// </summary>
public static class TenantAccessPolicy
{
    public static TenantAccessDecision Evaluate(TenantAccessState state)
    {
        if (!state.TenantExists)
            return Block("TENANT_NOT_FOUND", 404);

        // Workspace status always wins over subscription status.
        switch (state.TenantStatus)
        {
            case TenantStatus.Suspended:
                return state.SuspensionReason == SuspensionReason.NonPayment
                    ? Block("TENANT_SUSPENDED_NONPAYMENT", 402)
                    : Block("TENANT_SUSPENDED", 403);
            case TenantStatus.Archived:
                return Block("TENANT_ARCHIVED", 403);
            case TenantStatus.Deleted:
                return Block("TENANT_NOT_FOUND", 404);
            case TenantStatus.Provisioning:
                return Block("WORKSPACE_PROVISIONING", 423);
            case TenantStatus.ProvisioningFailed:
                return Block("WORKSPACE_PROVISIONING_FAILED", 503);
            case TenantStatus.PendingApproval:
            case TenantStatus.PendingSubscription:
            case TenantStatus.AwaitingDatabaseCapacity:
                return new TenantAccessDecision(TenantAccessMode.BillingOnly);
            // Legacy tenant-level cancellation is read-only under the new subscription policy.
            case TenantStatus.Cancelled:
                return new TenantAccessDecision(TenantAccessMode.ReadOnly);
        }

        // Existing gyms without a SaaS record preserve their legacy access during rollout. A new
        // FreelanceCoach workspace without a subscription is explicitly billing-only.
        if (state.SubscriptionStatus is null)
            return state.WorkspaceType == WorkspaceType.FreelanceCoach
                ? new TenantAccessDecision(TenantAccessMode.BillingOnly)
                : new TenantAccessDecision(TenantAccessMode.Full);

        return state.SubscriptionStatus switch
        {
            TenantSubscriptionStatus.None or TenantSubscriptionStatus.PendingPayment or TenantSubscriptionStatus.PendingActivation
                => new TenantAccessDecision(TenantAccessMode.BillingOnly),
            TenantSubscriptionStatus.Trial or TenantSubscriptionStatus.Active or TenantSubscriptionStatus.PastDue or TenantSubscriptionStatus.GracePeriod
                => new TenantAccessDecision(TenantAccessMode.Full),
            TenantSubscriptionStatus.Expired or TenantSubscriptionStatus.Cancelled or TenantSubscriptionStatus.Suspended
                => new TenantAccessDecision(TenantAccessMode.ReadOnly),
            _ => new TenantAccessDecision(TenantAccessMode.BillingOnly)
        };
    }

    /// <summary>Compatibility entry point for callers that only need a true hard block.</summary>
    public static TenantBlock? EvaluateHardBlock(TenantAccessState state) => Evaluate(state).Block;

    public static bool IsPendingApproval(TenantAccessState state) =>
        state.TenantExists && state.TenantStatus == TenantStatus.PendingApproval;

    private static TenantAccessDecision Block(string code, int httpStatus) =>
        new(TenantAccessMode.Blocked, new TenantBlock(code, httpStatus));
}
