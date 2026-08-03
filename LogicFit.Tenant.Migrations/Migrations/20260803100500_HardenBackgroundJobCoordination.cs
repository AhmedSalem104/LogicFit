using LogicFit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogicFit.TenantDatabase.Migrations.Migrations;

[DbContext(typeof(TenantDbContext))]
[Migration("20260803100500_HardenBackgroundJobCoordination")]
public partial class HardenBackgroundJobCoordination : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF EXISTS (
                SELECT 1
                FROM [OutboxMessages]
                GROUP BY [IdempotencyKey]
                HAVING COUNT_BIG(*) > 1)
            BEGIN
                THROW 51000, 'Duplicate OutboxMessages.IdempotencyKey values require operator review before the unique index can be applied.', 1;
            END
            """);

        migrationBuilder.AlterColumn<string>(
            name: "Type",
            table: "OutboxMessages",
            type: "nvarchar(200)",
            maxLength: 200,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(max)");

        migrationBuilder.AlterColumn<string>(
            name: "IdempotencyKey",
            table: "OutboxMessages",
            type: "nvarchar(200)",
            maxLength: 200,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(max)");

        migrationBuilder.CreateIndex(
            name: "IX_OutboxMessages_IdempotencyKey",
            table: "OutboxMessages",
            column: "IdempotencyKey",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_OutboxMessages_ProcessedAtUtc_OccurredAtUtc",
            table: "OutboxMessages",
            columns: new[] { "ProcessedAtUtc", "OccurredAtUtc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_OutboxMessages_IdempotencyKey",
            table: "OutboxMessages");

        migrationBuilder.DropIndex(
            name: "IX_OutboxMessages_ProcessedAtUtc_OccurredAtUtc",
            table: "OutboxMessages");

        migrationBuilder.AlterColumn<string>(
            name: "Type",
            table: "OutboxMessages",
            type: "nvarchar(max)",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(200)",
            oldMaxLength: 200);

        migrationBuilder.AlterColumn<string>(
            name: "IdempotencyKey",
            table: "OutboxMessages",
            type: "nvarchar(max)",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(200)",
            oldMaxLength: 200);
    }
}
