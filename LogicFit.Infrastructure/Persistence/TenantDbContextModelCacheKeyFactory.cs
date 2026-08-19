using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace LogicFit.Infrastructure.Persistence;

/// <summary>
/// Keeps the explicit TenantId in EF's model cache key so a query filter for one workspace can
/// never be reused for another workspace in the same process.
/// </summary>
public sealed class TenantDbContextModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime)
        => context is TenantDbContext tenant
            ? (context.GetType(), tenant.TenantId, designTime)
            : (context.GetType(), designTime);
}
