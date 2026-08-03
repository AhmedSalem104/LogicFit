using LogicFit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.TenantDatabase.Migrations;

/// <summary>Design-time wrapper so EF can target this migration assembly.</summary>
public sealed class TenantMigrationDbContext : TenantDbContext
{
    private static readonly Guid DesignTimeTenantId = Guid.Parse("2f0b6d1f-2d8f-4f32-b7d0-2cbde6afbf91");

    public TenantMigrationDbContext(DbContextOptions<TenantMigrationDbContext> options) : base(options, DesignTimeTenantId) { }
}
