using LogicFit.Application.Common.Interfaces;

namespace LogicFit.Infrastructure.Persistence;

/// <summary>
/// Request-scoped result of resolving the authenticated workspace against the platform mapping.
/// The decrypted connection string exists only in this scope and is never serialized or logged.
/// </summary>
public sealed class TenantDatabaseRequestScope
{
    public TenantDatabaseResolution? Resolution { get; private set; }

    public void Set(TenantDatabaseResolution resolution)
    {
        ArgumentNullException.ThrowIfNull(resolution);

        if (Resolution is not null && Resolution.TenantId != resolution.TenantId)
            throw new InvalidOperationException("A request cannot switch tenant database scopes.");

        Resolution = resolution;
    }

    public void Clear() => Resolution = null;
}
