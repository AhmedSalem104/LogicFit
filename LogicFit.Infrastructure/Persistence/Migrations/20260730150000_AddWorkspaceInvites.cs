using LogicFit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogicFit.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260730150000_AddWorkspaceInvites")]
public partial class AddWorkspaceInvites : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'dbo.WorkspaceInvites', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.WorkspaceInvites (
                    Id uniqueidentifier NOT NULL PRIMARY KEY, TenantId uniqueidentifier NOT NULL,
                    Email nvarchar(256) NOT NULL, NormalizedEmail nvarchar(256) NOT NULL,
                    Role int NOT NULL, InvitedByMembershipId uniqueidentifier NOT NULL,
                    TokenHash nvarchar(128) NOT NULL, ExpiresAt datetime2 NOT NULL, Status int NOT NULL,
                    AcceptedAt datetime2 NULL, AcceptedIdentityAccountId uniqueidentifier NULL, RevokedAt datetime2 NULL,
                    RowVersion rowversion NOT NULL, CreatedAt datetime2 NOT NULL, CreatedBy nvarchar(max) NULL,
                    UpdatedAt datetime2 NULL, UpdatedBy nvarchar(max) NULL,
                    CONSTRAINT FK_WorkspaceInvites_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(Id),
                    CONSTRAINT FK_WorkspaceInvites_Memberships FOREIGN KEY (InvitedByMembershipId) REFERENCES dbo.WorkspaceMemberships(Id));
            END;
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.WorkspaceInvites') AND name=N'IX_WorkspaceInvites_TokenHash')
                CREATE UNIQUE INDEX IX_WorkspaceInvites_TokenHash ON dbo.WorkspaceInvites(TokenHash);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.WorkspaceInvites') AND name=N'IX_WorkspaceInvites_ActiveEmailRole')
                CREATE UNIQUE INDEX IX_WorkspaceInvites_ActiveEmailRole ON dbo.WorkspaceInvites(TenantId, NormalizedEmail, Role, Status) WHERE Status = 1;
            """);
    }
    protected override void Down(MigrationBuilder migrationBuilder) { }
}
