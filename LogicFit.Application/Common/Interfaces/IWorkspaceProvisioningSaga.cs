using LogicFit.Domain.Enums;

namespace LogicFit.Application.Common.Interfaces;

public sealed record WorkspaceProvisioningOutcome(
    Guid TenantId,
    Guid ApplicationRequestId,
    ProvisioningJobStatus Status,
    Guid? DatabaseResourceId,
    string? ErrorCode = null);

/// <summary>Runs the persistent, retryable boundary between Platform approval and Tenant activation.</summary>
public interface IWorkspaceProvisioningSaga
{
    Task<WorkspaceProvisioningOutcome> RunAsync(Guid applicationRequestId, CancellationToken cancellationToken = default);
}
