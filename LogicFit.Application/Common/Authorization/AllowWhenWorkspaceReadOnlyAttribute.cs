namespace LogicFit.Application.Common.Authorization;

/// <summary>
/// Marks a mutation that remains available after a workspace subscription expires. Use only for
/// invoices, plan selection, renewal, and payment submission.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class AllowWhenWorkspaceReadOnlyAttribute : Attribute
{
}
