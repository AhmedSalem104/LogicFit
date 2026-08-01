using System.Reflection;
using LogicFit.Infrastructure.Persistence.Migrations;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

namespace LogicFit.Tests;

public class FreelanceSystemRolesMigrationTests
{
    [Fact]
    public void Seeds_all_freelance_roles_with_idempotent_permission_mappings()
    {
        var migration = new SeedFreelanceSystemRoles();
        var builder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");
        var up = typeof(SeedFreelanceSystemRoles).GetMethod("Up", BindingFlags.Instance | BindingFlags.NonPublic)!;

        up.Invoke(migration, [builder]);

        var sql = string.Join('\n', builder.Operations.OfType<SqlOperation>().Select(operation => operation.Sql));
        Assert.Contains("FreelanceOwner", sql);
        Assert.Contains("FreelanceCoach", sql);
        Assert.Contains("FreelanceAssistant", sql);
        Assert.Contains("NOT EXISTS", sql);
        Assert.Contains("RolePermissions", sql);
    }
}
