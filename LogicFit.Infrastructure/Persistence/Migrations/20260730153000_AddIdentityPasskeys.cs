using LogicFit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogicFit.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260730153000_AddIdentityPasskeys")]
public partial class AddIdentityPasskeys : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'dbo.IdentityPasskeyCredentials', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.IdentityPasskeyCredentials (
                    Id uniqueidentifier NOT NULL PRIMARY KEY, IdentityAccountId uniqueidentifier NOT NULL,
                    CredentialId varbinary(1024) NOT NULL, PublicKey varbinary(4096) NOT NULL, UserHandle varbinary(128) NOT NULL,
                    SignatureCounter bigint NOT NULL, FriendlyName nvarchar(120) NULL, LastUsedAt datetime2 NULL, IsActive bit NOT NULL,
                    RowVersion rowversion NOT NULL, CreatedAt datetime2 NOT NULL, CreatedBy nvarchar(max) NULL, UpdatedAt datetime2 NULL, UpdatedBy nvarchar(max) NULL,
                    CONSTRAINT FK_IdentityPasskeyCredentials_IdentityAccounts FOREIGN KEY (IdentityAccountId) REFERENCES dbo.IdentityAccounts(Id));
            END;
            IF OBJECT_ID(N'dbo.IdentityPasskeyCeremonies', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.IdentityPasskeyCeremonies (
                    Id uniqueidentifier NOT NULL PRIMARY KEY, IdentityAccountId uniqueidentifier NOT NULL, Purpose int NOT NULL,
                    OptionsJson nvarchar(max) NOT NULL, ExpiresAt datetime2 NOT NULL, UsedAt datetime2 NULL, RowVersion rowversion NOT NULL,
                    CreatedAt datetime2 NOT NULL, CreatedBy nvarchar(max) NULL, UpdatedAt datetime2 NULL, UpdatedBy nvarchar(max) NULL,
                    CONSTRAINT FK_IdentityPasskeyCeremonies_IdentityAccounts FOREIGN KEY (IdentityAccountId) REFERENCES dbo.IdentityAccounts(Id));
            END;
            IF OBJECT_ID(N'dbo.IdentityPasskeyStepUpSessions', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.IdentityPasskeyStepUpSessions (
                    Id uniqueidentifier NOT NULL PRIMARY KEY, IdentityAccountId uniqueidentifier NOT NULL, TokenHash nvarchar(128) NOT NULL,
                    ExpiresAt datetime2 NOT NULL, RevokedAt datetime2 NULL, RowVersion rowversion NOT NULL,
                    CreatedAt datetime2 NOT NULL, CreatedBy nvarchar(max) NULL, UpdatedAt datetime2 NULL, UpdatedBy nvarchar(max) NULL,
                    CONSTRAINT FK_IdentityPasskeyStepUpSessions_IdentityAccounts FOREIGN KEY (IdentityAccountId) REFERENCES dbo.IdentityAccounts(Id));
            END;
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.IdentityPasskeyCredentials') AND name=N'IX_IdentityPasskeyCredentials_CredentialId')
                CREATE UNIQUE INDEX IX_IdentityPasskeyCredentials_CredentialId ON dbo.IdentityPasskeyCredentials(CredentialId);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.IdentityPasskeyCredentials') AND name=N'IX_IdentityPasskeyCredentials_IdentityAccountId_IsActive')
                CREATE INDEX IX_IdentityPasskeyCredentials_IdentityAccountId_IsActive ON dbo.IdentityPasskeyCredentials(IdentityAccountId, IsActive);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.IdentityPasskeyCeremonies') AND name=N'IX_IdentityPasskeyCeremonies_IdentityAccountId_Purpose_ExpiresAt')
                CREATE INDEX IX_IdentityPasskeyCeremonies_IdentityAccountId_Purpose_ExpiresAt ON dbo.IdentityPasskeyCeremonies(IdentityAccountId, Purpose, ExpiresAt);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.IdentityPasskeyStepUpSessions') AND name=N'IX_IdentityPasskeyStepUpSessions_TokenHash')
                CREATE UNIQUE INDEX IX_IdentityPasskeyStepUpSessions_TokenHash ON dbo.IdentityPasskeyStepUpSessions(TokenHash);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.IdentityPasskeyStepUpSessions') AND name=N'IX_IdentityPasskeyStepUpSessions_IdentityAccountId_ExpiresAt')
                CREATE INDEX IX_IdentityPasskeyStepUpSessions_IdentityAccountId_ExpiresAt ON dbo.IdentityPasskeyStepUpSessions(IdentityAccountId, ExpiresAt);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder) { }
}
