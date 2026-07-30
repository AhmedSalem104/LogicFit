using LogicFit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogicFit.Infrastructure.Persistence.Migrations;

/// <summary>
/// Adds email-verification/reset security records. This migration is intentionally guarded for
/// the existing production schemas where IdentityAccounts may already have been provisioned.
/// </summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260730143000_AddIdentityEmailSecurity")]
public partial class AddIdentityEmailSecurity : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF COL_LENGTH(N'dbo.IdentityAccounts', N'FullName') IS NULL
                ALTER TABLE dbo.IdentityAccounts ADD FullName nvarchar(200) NULL;

            IF COL_LENGTH(N'dbo.IdentityAccounts', N'EmailVerifiedAt') IS NULL
                ALTER TABLE dbo.IdentityAccounts ADD EmailVerifiedAt datetime2 NULL;

            UPDATE dbo.IdentityAccounts
            SET FullName = CASE
                    WHEN FullName IS NULL OR LTRIM(RTRIM(FullName)) = N'' THEN Email
                    ELSE FullName
                END,
                EmailVerifiedAt = COALESCE(EmailVerifiedAt, CreatedAt, SYSUTCDATETIME())
            WHERE FullName IS NULL
               OR LTRIM(RTRIM(FullName)) = N''
               OR EmailVerifiedAt IS NULL;

            IF EXISTS (
                SELECT 1
                FROM sys.columns
                WHERE object_id = OBJECT_ID(N'dbo.IdentityAccounts')
                  AND name = N'FullName'
                  AND is_nullable = 1)
                ALTER TABLE dbo.IdentityAccounts ALTER COLUMN FullName nvarchar(200) NOT NULL;
            """);

        migrationBuilder.Sql("""
            IF OBJECT_ID(N'dbo.IdentityEmailActionTokens', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.IdentityEmailActionTokens
                (
                    Id uniqueidentifier NOT NULL,
                    IdentityAccountId uniqueidentifier NOT NULL,
                    Purpose int NOT NULL,
                    TokenHash nvarchar(128) NOT NULL,
                    ExpiresAt datetime2 NOT NULL,
                    UsedAt datetime2 NULL,
                    RevokedAt datetime2 NULL,
                    CreatedByIp nvarchar(64) NULL,
                    RowVersion rowversion NOT NULL,
                    CreatedAt datetime2 NOT NULL,
                    CreatedBy nvarchar(max) NULL,
                    UpdatedAt datetime2 NULL,
                    UpdatedBy nvarchar(max) NULL,
                    CONSTRAINT PK_IdentityEmailActionTokens PRIMARY KEY (Id),
                    CONSTRAINT FK_IdentityEmailActionTokens_IdentityAccounts_IdentityAccountId
                        FOREIGN KEY (IdentityAccountId) REFERENCES dbo.IdentityAccounts(Id) ON DELETE NO ACTION
                );
            END;

            IF NOT EXISTS (
                SELECT 1 FROM sys.indexes
                WHERE object_id = OBJECT_ID(N'dbo.IdentityEmailActionTokens')
                  AND name = N'IX_IdentityEmailActionTokens_TokenHash')
                CREATE UNIQUE INDEX IX_IdentityEmailActionTokens_TokenHash
                    ON dbo.IdentityEmailActionTokens(TokenHash);

            IF NOT EXISTS (
                SELECT 1 FROM sys.indexes
                WHERE object_id = OBJECT_ID(N'dbo.IdentityEmailActionTokens')
                  AND name = N'IX_IdentityEmailActionTokens_IdentityAccountId_Purpose_ExpiresAt')
                CREATE INDEX IX_IdentityEmailActionTokens_IdentityAccountId_Purpose_ExpiresAt
                    ON dbo.IdentityEmailActionTokens(IdentityAccountId, Purpose, ExpiresAt);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Deliberately non-destructive: removing hashes can make active verification/reset links
        // unverifiable and deleting profile data is not a safe automatic rollback operation.
    }
}
