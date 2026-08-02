using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogicFit.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemovePostLoginOtpStepUp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[OtpStepUpSessions]', N'U') IS NOT NULL
                    DROP TABLE [OtpStepUpSessions];
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
                END;

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
    }
}
