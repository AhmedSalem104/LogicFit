using LogicFit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogicFit.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260824130000_ReconcileProductionSchemaIndexes")]

/// <summary>
/// Reconciles the manually-created WorkspaceInvites table with the EF model without touching
/// existing invite data or removing the legacy ApplicationRequests uniqueness rule.
/// </summary>
public partial class ReconcileProductionSchemaIndexes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'dbo.WorkspaceInvites', N'U') IS NOT NULL
               AND NOT EXISTS
               (
                   SELECT 1
                   FROM sys.indexes
                   WHERE object_id = OBJECT_ID(N'dbo.WorkspaceInvites')
                     AND name = N'IX_WorkspaceInvites_InvitedByMembershipId'
               )
            BEGIN
                CREATE INDEX IX_WorkspaceInvites_InvitedByMembershipId
                    ON dbo.WorkspaceInvites(InvitedByMembershipId);
            END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'dbo.WorkspaceInvites', N'U') IS NOT NULL
               AND EXISTS
               (
                   SELECT 1
                   FROM sys.indexes
                   WHERE object_id = OBJECT_ID(N'dbo.WorkspaceInvites')
                     AND name = N'IX_WorkspaceInvites_InvitedByMembershipId'
               )
            BEGIN
                DROP INDEX IX_WorkspaceInvites_InvitedByMembershipId ON dbo.WorkspaceInvites;
            END;
            """);
    }
}
