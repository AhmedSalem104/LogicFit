using System.Data;
using System.Reflection;
using LogicFit.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace LogicFit.Infrastructure.Persistence;

/// <summary>
/// Compatibility bridge for existing handlers. Platform-owned sets use PlatformDbContext;
/// tenant-owned sets use the resolved TenantDbContext. A tenant request can never fall back to
/// the legacy shared ApplicationDbContext.
/// </summary>
public class TenantAwareApplicationDbContextProxy : DispatchProxy
{
    private PlatformDbContext? _platformContext;
    private ApplicationDbContext? _legacyContext;
    private TenantDatabaseRequestScope? _requestScope;
    private TenantDatabaseContextAccessor? _tenantContextAccessor;
    private ITenantService? _tenantService;

    public static IApplicationDbContext Create(
        PlatformDbContext platformContext,
        ApplicationDbContext legacyContext,
        TenantDatabaseRequestScope requestScope,
        TenantDatabaseContextAccessor tenantContextAccessor,
        ITenantService tenantService)
    {
        var proxy = DispatchProxy.Create<IApplicationDbContext, TenantAwareApplicationDbContextProxy>();
        ((TenantAwareApplicationDbContextProxy)(object)proxy).Initialize(
            platformContext,
            legacyContext,
            requestScope,
            tenantContextAccessor,
            tenantService);
        return proxy;
    }

    private void Initialize(
        PlatformDbContext platformContext,
        ApplicationDbContext legacyContext,
        TenantDatabaseRequestScope requestScope,
        TenantDatabaseContextAccessor tenantContextAccessor,
        ITenantService tenantService)
    {
        _platformContext = platformContext;
        _legacyContext = legacyContext;
        _requestScope = requestScope;
        _tenantContextAccessor = tenantContextAccessor;
        _tenantService = tenantService;
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        if (targetMethod is null)
            throw new InvalidOperationException("The database context proxy received an empty method.");

        if (targetMethod.Name.StartsWith("get_", StringComparison.Ordinal) &&
            targetMethod.ReturnType.IsGenericType &&
            targetMethod.ReturnType.GetGenericTypeDefinition() == typeof(DbSet<>))
        {
            var entityType = targetMethod.ReturnType.GetGenericArguments()[0];
            return Set(ResolveContext(entityType), entityType);
        }

        return targetMethod.Name switch
        {
            nameof(IApplicationDbContext.Entry) => ResolveContext(args?[0]?.GetType() ?? typeof(object))
                .Entry(args?[0] ?? throw new ArgumentNullException(nameof(args))),
            nameof(IApplicationDbContext.SaveChangesAsync) => SaveChangesAsync(GetArgument<CancellationToken>(args, 0)),
            nameof(IApplicationDbContext.BeginTransactionAsync) when targetMethod.GetParameters().Length == 1
                => BeginTransactionAsync(GetArgument<CancellationToken>(args, 0)),
            nameof(IApplicationDbContext.BeginTransactionAsync) => BeginTransactionAsync(
                GetArgument<IsolationLevel>(args, 0),
                GetArgument<CancellationToken>(args, 1)),
            _ => throw new NotSupportedException($"The database context member '{targetMethod.Name}' is not supported.")
        };
    }

    private DbContext ResolveContext(Type entityType)
    {
        var platform = _platformContext ?? throw new InvalidOperationException("Platform context is not initialized.");
        var legacy = _legacyContext ?? throw new InvalidOperationException("Legacy context is not initialized.");
        var tenantService = _tenantService ?? throw new InvalidOperationException("Tenant service is not initialized.");
        var requestScope = _requestScope ?? throw new InvalidOperationException("Tenant request scope is not initialized.");
        var tenantAccessor = _tenantContextAccessor ?? throw new InvalidOperationException("Tenant context accessor is not initialized.");

        var isTenantOwned = DbContextOwnership.TenantEntities.Contains(entityType);
        var isPlatformOwned = DbContextOwnership.PlatformEntities.Contains(entityType);
        var hasResolvedTenant = tenantService.CurrentTenantId.HasValue && requestScope.Resolution is not null;

        if (isTenantOwned && hasResolvedTenant)
            return tenantAccessor.GetRequiredContext();

        if (isPlatformOwned)
            return platform;

        if (isTenantOwned)
        {
            if (tenantService.CurrentTenantId.HasValue)
                throw new InvalidOperationException("The tenant database mapping is unavailable; shared database fallback is disabled.");

            return legacy;
        }

        throw new InvalidOperationException($"Entity '{entityType.Name}' has no database owner.");
    }

    private static object Set(DbContext context, Type entityType)
    {
        var setMethod = typeof(DbContext)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Single(method => method.Name == nameof(DbContext.Set) && method.IsGenericMethodDefinition && method.GetParameters().Length == 0);
        return setMethod.MakeGenericMethod(entityType).Invoke(context, null)
            ?? throw new InvalidOperationException($"Could not create a DbSet for '{entityType.Name}'.");
    }

    private static T GetArgument<T>(object?[]? args, int index)
        => args is not null && index < args.Length && args[index] is T value ? value : default!;

    private Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        => SaveAllAsync(_platformContext!, _legacyContext!, _tenantContextAccessor!.Current, cancellationToken);

    private static async Task<int> SaveAllAsync(
        PlatformDbContext platform,
        ApplicationDbContext legacy,
        TenantDbContext? tenant,
        CancellationToken cancellationToken)
    {
        var affected = 0;
        if (platform.ChangeTracker.HasChanges())
            affected += await platform.SaveChangesAsync(cancellationToken);
        if (legacy.ChangeTracker.HasChanges())
            affected += await legacy.SaveChangesAsync(cancellationToken);
        if (tenant is not null && tenant.ChangeTracker.HasChanges())
            affected += await tenant.SaveChangesAsync(cancellationToken);
        return affected;
    }

    private Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken)
        => BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

    private async Task<IDbContextTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken)
    {
        var transactions = new List<IDbContextTransaction>();
        try
        {
            transactions.Add(await _platformContext!.Database.BeginTransactionAsync(isolationLevel, cancellationToken));
            if (_requestScope!.Resolution is not null)
                transactions.Add(await _tenantContextAccessor!.GetRequiredContext().Database.BeginTransactionAsync(isolationLevel, cancellationToken));
            else
                transactions.Add(await _legacyContext!.Database.BeginTransactionAsync(isolationLevel, cancellationToken));

            return new CompositeDbContextTransaction(transactions);
        }
        catch
        {
            foreach (var transaction in transactions.AsEnumerable().Reverse())
                await transaction.DisposeAsync();
            throw;
        }
    }

    private sealed class CompositeDbContextTransaction(IReadOnlyList<IDbContextTransaction> transactions) : IDbContextTransaction
    {
        private bool _completed;
        public Guid TransactionId { get; } = Guid.NewGuid();

        public void Commit()
        {
            foreach (var transaction in transactions) transaction.Commit();
            _completed = true;
        }

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            foreach (var transaction in transactions) await transaction.CommitAsync(cancellationToken);
            _completed = true;
        }

        public void Rollback()
        {
            foreach (var transaction in transactions.AsEnumerable().Reverse()) transaction.Rollback();
            _completed = true;
        }

        public async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            foreach (var transaction in transactions.AsEnumerable().Reverse()) await transaction.RollbackAsync(cancellationToken);
            _completed = true;
        }

        public System.Data.Common.DbTransaction GetDbTransaction()
            => throw new NotSupportedException("A composite transaction spans multiple databases and has no single DbTransaction.");

        public void Dispose()
        {
            if (!_completed)
                foreach (var transaction in transactions.AsEnumerable().Reverse()) transaction.Rollback();
            foreach (var transaction in transactions.AsEnumerable().Reverse()) transaction.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            if (!_completed)
                foreach (var transaction in transactions.AsEnumerable().Reverse()) await transaction.RollbackAsync();
            foreach (var transaction in transactions.AsEnumerable().Reverse()) await transaction.DisposeAsync();
            GC.SuppressFinalize(this);
        }
    }
}
