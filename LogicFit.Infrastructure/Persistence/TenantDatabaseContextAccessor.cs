namespace LogicFit.Infrastructure.Persistence;

/// <summary>
/// Creates exactly one TenantDbContext for the resolved workspace during a request.  The
/// connection string comes exclusively from the server-side mapping resolver.
/// </summary>
public sealed class TenantDatabaseContextAccessor(
    TenantDatabaseRequestScope requestScope) : IDisposable, IAsyncDisposable
{
    private TenantDbContext? _context;

    public TenantDbContext GetRequiredContext()
    {
        var resolution = requestScope.Resolution
            ?? throw new InvalidOperationException(
                "The tenant database has not been resolved for this request.");

        if (_context is not null)
        {
            if (_context.TenantId != resolution.TenantId)
                throw new InvalidOperationException("The tenant database scope changed during the request.");

            return _context;
        }

        _context = TenantRuntimeDbContextFactory.Create(resolution);
        return _context;
    }

    public TenantDbContext? Current => _context;

    public void Dispose()
    {
        _context?.Dispose();
        _context = null;
    }

    public async ValueTask DisposeAsync()
    {
        if (_context is not null)
            await _context.DisposeAsync();

        _context = null;
        GC.SuppressFinalize(this);
    }
}
