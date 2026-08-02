using LogicFit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Platform.Migrations;

/// <summary>Design-time wrapper so EF can target this migration assembly.</summary>
public sealed class PlatformMigrationDbContext : PlatformDbContext
{
    public PlatformMigrationDbContext(DbContextOptions<PlatformMigrationDbContext> options) : base(options) { }
}
