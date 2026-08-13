using System.Data.Common;
using LogicFit.Application.Common.Interfaces;
using LogicFit.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace LogicFit.Infrastructure.Services;

/// <summary>
/// Resolves and installs the tenant database scope for the anonymous identity-to-workspace
/// exchange. A selection request has no tenant JWT yet, so it cannot rely on the normal request
/// middleware to establish the tenant context.
/// </summary>
public sealed class WorkspaceDatabaseScope(
    ITenantDatabaseResolver resolver,
    ITenantService tenantService,
    TenantDatabaseRequestScope requestScope,
    ILogger<WorkspaceDatabaseScope> logger) : IWorkspaceDatabaseScope
{
    public async Task<bool> TryOpenAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty)
            return false;

        try
        {
            var resolution = await resolver.ResolveAsync(tenantId, cancellationToken);
            if (resolution is null)
                return false;

            await tenantService.SetTenantAsync(tenantId);
            requestScope.Set(resolution);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (DbException exception)
        {
            // A mapping or platform connection can be temporarily unavailable. Keep the
            // protected connection material out of logs and let the caller return a typed 503.
            logger.LogError(
                exception,
                "Workspace database resolution failed for TenantId {TenantId}.",
                tenantId);
            return false;
        }
        catch (TimeoutException exception)
        {
            logger.LogError(
                exception,
                "Workspace database resolution timed out for TenantId {TenantId}.",
                tenantId);
            return false;
        }
    }

    public void Close()
    {
        requestScope.Clear();
        tenantService.ClearTenant();
    }
}
