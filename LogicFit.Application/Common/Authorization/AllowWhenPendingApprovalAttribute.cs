namespace LogicFit.Application.Common.Authorization;

/// <summary>
/// Marks an endpoint (controller or action) as reachable while the gym is still <c>PendingApproval</c>.
/// Used by the tenant access authorization policy to allow onboarding/billing endpoints before the gym
/// is approved, while every other endpoint is blocked with TENANT_PENDING_APPROVAL.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class AllowWhenPendingApprovalAttribute : Attribute
{
}
