namespace LogicFit.Application.Common.Interfaces;

/// <summary>
/// Opens the server-resolved database scope for one workspace during an identity workspace
/// selection. The implementation must never accept database identifiers or connection strings
/// from the HTTP request.
/// </summary>
public interface IWorkspaceDatabaseScope
{
    Task<bool> TryOpenAsync(Guid tenantId, CancellationToken cancellationToken = default);

    void Close();
}
