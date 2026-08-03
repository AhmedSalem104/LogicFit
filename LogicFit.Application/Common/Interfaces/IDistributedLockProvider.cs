namespace LogicFit.Application.Common.Interfaces;

/// <summary>
/// Coordinates singleton background work across API instances.
/// A null lease means another instance owns the lock and this pass should be skipped.
/// </summary>
public interface IDistributedLockProvider
{
    Task<IAsyncDisposable?> TryAcquireAsync(string resource, CancellationToken cancellationToken = default);
}
