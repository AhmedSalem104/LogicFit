using LogicFit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogicFit.Platform.Migrations.Migrations;

[DbContext(typeof(PlatformDbContext))]
[Migration("20260803160000_RemoveLegacyOtpArtifacts")]
public partial class RemoveLegacyOtpArtifacts : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'[OtpChallenge]', N'U') IS NOT NULL
                DROP TABLE [OtpChallenge];
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "OtpChallenge",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                IdentityAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                AttemptCount = table.Column<int>(type: "int", nullable: false),
                CodeHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                CodeSalt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                ConsumedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                DeliveryStatus = table.Column<int>(type: "int", nullable: false),
                ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                LastSentAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                MaxAttempts = table.Column<int>(type: "int", nullable: false),
                NormalizedPhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Provider = table.Column<string>(type: "nvarchar(max)", nullable: false),
                ProviderMessageId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                Purpose = table.Column<int>(type: "int", nullable: false),
                ResendCount = table.Column<int>(type: "int", nullable: false),
                RevokedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                SessionBinding = table.Column<string>(type: "nvarchar(max)", nullable: true),
                Status = table.Column<int>(type: "int", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_OtpChallenge", x => x.Id);
                table.ForeignKey(
                    name: "FK_OtpChallenge_IdentityAccounts_IdentityAccountId",
                    column: x => x.IdentityAccountId,
                    principalTable: "IdentityAccounts",
                    principalColumn: "Id");
            });

        migrationBuilder.CreateIndex(
            name: "IX_OtpChallenge_IdentityAccountId",
            table: "OtpChallenge",
            column: "IdentityAccountId");
    }
}
