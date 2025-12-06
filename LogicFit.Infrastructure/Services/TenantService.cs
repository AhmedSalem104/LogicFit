using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LogicFit.Infrastructure.Services;

public class TenantService : ITenantService
{
    private Guid? _currentTenantId;
    private readonly IServiceProvider _serviceProvider;

    public TenantService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Guid? CurrentTenantId => _currentTenantId;

    public Task SetTenantAsync(Guid tenantId)
    {
        _currentTenantId = tenantId;
        return Task.CompletedTask;
    }

    public async Task SetTenantBySubdomainAsync(string subdomain)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var tenant = await dbContext.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Subdomain == subdomain && !t.IsDeleted);

        if (tenant != null)
        {
            _currentTenantId = tenant.Id;
        }
    }

    public async Task<bool> TenantExistsAsync(Guid tenantId)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await dbContext.Tenants
            .IgnoreQueryFilters()
            .AnyAsync(t => t.Id == tenantId && !t.IsDeleted);
    }
}
