using LogicFit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogicFit.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260730151000_AddWorkspaceClientJoinCodes")]
public partial class AddWorkspaceClientJoinCodes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'dbo.WorkspaceClientJoinCodes', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.WorkspaceClientJoinCodes (
                    Id uniqueidentifier NOT NULL PRIMARY KEY, TenantId uniqueidentifier NOT NULL,
                    CodeHash nvarchar(128) NOT NULL, ExpiresAt datetime2 NOT NULL, AutoApproveClients bit NOT NULL,
                    RevokedAt datetime2 NULL, RowVersion rowversion NOT NULL,
                    CreatedAt datetime2 NOT NULL, CreatedBy nvarchar(max) NULL, UpdatedAt datetime2 NULL, UpdatedBy nvarchar(max) NULL,
                    CONSTRAINT FK_WorkspaceClientJoinCodes_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(Id));
            END;
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.WorkspaceClientJoinCodes') AND name=N'IX_WorkspaceClientJoinCodes_CodeHash')
                CREATE UNIQUE INDEX IX_WorkspaceClientJoinCodes_CodeHash ON dbo.WorkspaceClientJoinCodes(CodeHash);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.WorkspaceClientJoinCodes') AND name=N'IX_WorkspaceClientJoinCodes_OneActivePerWorkspace')
                CREATE UNIQUE INDEX IX_WorkspaceClientJoinCodes_OneActivePerWorkspace ON dbo.WorkspaceClientJoinCodes(TenantId, RevokedAt) WHERE RevokedAt IS NULL;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder) { }
}
