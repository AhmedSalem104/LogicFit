using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogicFit.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceIdentityPasskeysWithCentralizedOtp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Historical environments may have received the old schema through an operator
            // script. Conditional drops keep this reviewed replacement safe if one table is absent.
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[IdentityPasskeyCeremonies]', N'U') IS NOT NULL
                    DROP TABLE [IdentityPasskeyCeremonies];
                IF OBJECT_ID(N'[IdentityPasskeyCredentials]', N'U') IS NOT NULL
                    DROP TABLE [IdentityPasskeyCredentials];
                IF OBJECT_ID(N'[IdentityPasskeyStepUpSessions]', N'U') IS NOT NULL
                    DROP TABLE [IdentityPasskeyStepUpSessions];
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH(N'RefreshTokens', N'RowVersion') IS NULL
                    ALTER TABLE [RefreshTokens] ADD [RowVersion] rowversion NOT NULL;

                IF COL_LENGTH(N'IdentityAccounts', N'FailedLoginAttempts') IS NULL
                    ALTER TABLE [IdentityAccounts] ADD [FailedLoginAttempts] int NOT NULL
                        CONSTRAINT [DF_IdentityAccounts_FailedLoginAttempts] DEFAULT (0);
                IF COL_LENGTH(N'IdentityAccounts', N'LockoutEndUtc') IS NULL
                    ALTER TABLE [IdentityAccounts] ADD [LockoutEndUtc] datetime2 NULL;
                IF COL_LENGTH(N'IdentityAccounts', N'PhoneVerifiedAt') IS NULL
                    ALTER TABLE [IdentityAccounts] ADD [PhoneVerifiedAt] datetime2 NULL;
                IF COL_LENGTH(N'IdentityAccounts', N'RowVersion') IS NULL
                    ALTER TABLE [IdentityAccounts] ADD [RowVersion] rowversion NOT NULL;

                IF COL_LENGTH(N'DomainUsers', N'FailedLoginAttempts') IS NULL
                    ALTER TABLE [DomainUsers] ADD [FailedLoginAttempts] int NOT NULL
                        CONSTRAINT [DF_DomainUsers_FailedLoginAttempts] DEFAULT (0);
                IF COL_LENGTH(N'DomainUsers', N'LockoutEndUtc') IS NULL
                    ALTER TABLE [DomainUsers] ADD [LockoutEndUtc] datetime2 NULL;
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[OtpChallenges]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [OtpChallenges] (
                        [Id] uniqueidentifier NOT NULL,
                        [IdentityAccountId] uniqueidentifier NULL,
                        [NormalizedPhoneNumber] nvarchar(16) NOT NULL,
                        [Purpose] int NOT NULL,
                        [CodeHash] nvarchar(128) NOT NULL,
                        [CodeSalt] nvarchar(128) NOT NULL,
                        [ExpiresAtUtc] datetime2 NOT NULL,
                        [AttemptCount] int NOT NULL,
                        [MaxAttempts] int NOT NULL,
                        [ResendCount] int NOT NULL,
                        [LastSentAtUtc] datetime2 NOT NULL,
                        [ConsumedAtUtc] datetime2 NULL,
                        [RevokedAtUtc] datetime2 NULL,
                        [Status] int NOT NULL,
                        [Provider] nvarchar(32) NOT NULL,
                        [ProviderMessageId] nvarchar(256) NULL,
                        [DeliveryStatus] int NOT NULL,
                        [CreatedAtUtc] datetime2 NOT NULL,
                        [SessionBinding] nvarchar(128) NULL,
                        [RowVersion] rowversion NOT NULL,
                        [CreatedAt] datetime2 NOT NULL,
                        [CreatedBy] nvarchar(max) NULL,
                        [UpdatedAt] datetime2 NULL,
                        [UpdatedBy] nvarchar(max) NULL,
                        CONSTRAINT [PK_OtpChallenges] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_OtpChallenges_IdentityAccounts_IdentityAccountId]
                            FOREIGN KEY ([IdentityAccountId]) REFERENCES [IdentityAccounts] ([Id]) ON DELETE NO ACTION
                    );
                END
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[OtpStepUpSessions]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [OtpStepUpSessions] (
                        [Id] uniqueidentifier NOT NULL,
                        [IdentityAccountId] uniqueidentifier NOT NULL,
                        [OtpChallengeId] uniqueidentifier NOT NULL,
                        [Purpose] int NOT NULL,
                        [TokenHash] nvarchar(128) NOT NULL,
                        [SessionBinding] nvarchar(128) NULL,
                        [ExpiresAtUtc] datetime2 NOT NULL,
                        [UsedAtUtc] datetime2 NULL,
                        [RevokedAtUtc] datetime2 NULL,
                        [RowVersion] rowversion NOT NULL,
                        [CreatedAt] datetime2 NOT NULL,
                        [CreatedBy] nvarchar(max) NULL,
                        [UpdatedAt] datetime2 NULL,
                        [UpdatedBy] nvarchar(max) NULL,
                        CONSTRAINT [PK_OtpStepUpSessions] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_OtpStepUpSessions_IdentityAccounts_IdentityAccountId]
                            FOREIGN KEY ([IdentityAccountId]) REFERENCES [IdentityAccounts] ([Id]) ON DELETE NO ACTION,
                        CONSTRAINT [FK_OtpStepUpSessions_OtpChallenges_OtpChallengeId]
                            FOREIGN KEY ([OtpChallengeId]) REFERENCES [OtpChallenges] ([Id]) ON DELETE NO ACTION
                    );
                END
                """);

            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_OtpChallenges_IdentityAccountId'
                    AND [object_id] = OBJECT_ID(N'[OtpChallenges]'))
                    CREATE INDEX [IX_OtpChallenges_IdentityAccountId]
                        ON [OtpChallenges] ([IdentityAccountId]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_OtpChallenges_NormalizedPhoneNumber_Purpose_Status'
                    AND [object_id] = OBJECT_ID(N'[OtpChallenges]'))
                    CREATE INDEX [IX_OtpChallenges_NormalizedPhoneNumber_Purpose_Status]
                        ON [OtpChallenges] ([NormalizedPhoneNumber], [Purpose], [Status]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_OtpChallenges_ProviderMessageId'
                    AND [object_id] = OBJECT_ID(N'[OtpChallenges]'))
                    CREATE INDEX [IX_OtpChallenges_ProviderMessageId]
                        ON [OtpChallenges] ([ProviderMessageId]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_OtpStepUpSessions_IdentityAccountId_ExpiresAtUtc'
                    AND [object_id] = OBJECT_ID(N'[OtpStepUpSessions]'))
                    CREATE INDEX [IX_OtpStepUpSessions_IdentityAccountId_ExpiresAtUtc]
                        ON [OtpStepUpSessions] ([IdentityAccountId], [ExpiresAtUtc]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_OtpStepUpSessions_OtpChallengeId'
                    AND [object_id] = OBJECT_ID(N'[OtpStepUpSessions]'))
                    CREATE INDEX [IX_OtpStepUpSessions_OtpChallengeId]
                        ON [OtpStepUpSessions] ([OtpChallengeId]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_OtpStepUpSessions_TokenHash'
                    AND [object_id] = OBJECT_ID(N'[OtpStepUpSessions]'))
                    CREATE UNIQUE INDEX [IX_OtpStepUpSessions_TokenHash]
                        ON [OtpStepUpSessions] ([TokenHash]);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OtpStepUpSessions");

            migrationBuilder.DropTable(
                name: "OtpChallenges");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "FailedLoginAttempts",
                table: "IdentityAccounts");

            migrationBuilder.DropColumn(
                name: "LockoutEndUtc",
                table: "IdentityAccounts");

            migrationBuilder.DropColumn(
                name: "PhoneVerifiedAt",
                table: "IdentityAccounts");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "IdentityAccounts");

            migrationBuilder.DropColumn(
                name: "FailedLoginAttempts",
                table: "DomainUsers");

            migrationBuilder.DropColumn(
                name: "LockoutEndUtc",
                table: "DomainUsers");

            migrationBuilder.CreateTable(
                name: "IdentityPasskeyCeremonies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdentityAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OptionsJson = table.Column<string>(type: "nvarchar(max)", maxLength: 16000, nullable: false),
                    Purpose = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UsedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdentityPasskeyCeremonies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IdentityPasskeyCeremonies_IdentityAccounts_IdentityAccountId",
                        column: x => x.IdentityAccountId,
                        principalTable: "IdentityAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IdentityPasskeyCredentials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdentityAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CredentialId = table.Column<byte[]>(type: "varbinary(1024)", maxLength: 1024, nullable: false),
                    FriendlyName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PublicKey = table.Column<byte[]>(type: "varbinary(4096)", maxLength: 4096, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    SignatureCounter = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserHandle = table.Column<byte[]>(type: "varbinary(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdentityPasskeyCredentials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IdentityPasskeyCredentials_IdentityAccounts_IdentityAccountId",
                        column: x => x.IdentityAccountId,
                        principalTable: "IdentityAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IdentityPasskeyStepUpSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdentityAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdentityPasskeyStepUpSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IdentityPasskeyStepUpSessions_IdentityAccounts_IdentityAccountId",
                        column: x => x.IdentityAccountId,
                        principalTable: "IdentityAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IdentityPasskeyCeremonies_IdentityAccountId_Purpose_ExpiresAt",
                table: "IdentityPasskeyCeremonies",
                columns: new[] { "IdentityAccountId", "Purpose", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_IdentityPasskeyCredentials_CredentialId",
                table: "IdentityPasskeyCredentials",
                column: "CredentialId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdentityPasskeyCredentials_IdentityAccountId_IsActive",
                table: "IdentityPasskeyCredentials",
                columns: new[] { "IdentityAccountId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_IdentityPasskeyStepUpSessions_IdentityAccountId_ExpiresAt",
                table: "IdentityPasskeyStepUpSessions",
                columns: new[] { "IdentityAccountId", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_IdentityPasskeyStepUpSessions_TokenHash",
                table: "IdentityPasskeyStepUpSessions",
                column: "TokenHash",
                unique: true);
        }
    }
}
