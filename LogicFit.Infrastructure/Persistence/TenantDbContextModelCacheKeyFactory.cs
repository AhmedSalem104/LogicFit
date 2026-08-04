using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace LogicFit.Infrastructure.Persistence;

/// <summary>
/// TenantId participates in the EF model cache key because the TenantDbContext query boundary
/// is built from the context's explicit tenant scope. Without this, the first workspace model
/// created in a process could reuse its filter for a different workspace.
/// </summary>
public sealed class TenantDbContextModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime)
        => context is TenantDbContext tenant
            ? (context.GetType(), tenant.TenantId, designTime)
            : (context.GetType(), designTime);
}
