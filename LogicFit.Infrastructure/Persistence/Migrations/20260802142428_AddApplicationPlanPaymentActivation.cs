using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogicFit.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicationPlanPaymentActivation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ApplicationRequestId",
                table: "PaymentRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BillingCycle",
                table: "PaymentRequests",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "PaymentRequests",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "IdentityAccountId",
                table: "PaymentRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlanSnapshotJson",
                table: "PaymentRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BillingCycle",
                table: "ApplicationRequests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PlanId",
                table: "ApplicationRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PlanSnapshotAtUtc",
                table: "ApplicationRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlanSnapshotJson",
                table: "ApplicationRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PaymentProofs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    StorageKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Sha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false),
                    UploadedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UploadedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentProofs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentProofs_PaymentRequests_PaymentRequestId",
                        column: x => x.PaymentRequestId,
                        principalTable: "PaymentRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRequests_ApplicationRequestId",
                table: "PaymentRequests",
                column: "ApplicationRequestId",
                unique: true,
                filter: "[ApplicationRequestId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRequests_IdempotencyKey",
                table: "PaymentRequests",
                column: "IdempotencyKey",
                unique: true,
                filter: "[IdempotencyKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationRequests_PlanId",
                table: "ApplicationRequests",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentProofs_PaymentRequestId_IsCurrent",
                table: "PaymentProofs",
                columns: new[] { "PaymentRequestId", "IsCurrent" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentProofs_PaymentRequestId_Version",
                table: "PaymentProofs",
                columns: new[] { "PaymentRequestId", "Version" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationRequests_Plans_PlanId",
                table: "ApplicationRequests",
                column: "PlanId",
                principalTable: "Plans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentRequests_ApplicationRequests_ApplicationRequestId",
                table: "PaymentRequests",
                column: "ApplicationRequestId",
                principalTable: "ApplicationRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApplicationRequests_Plans_PlanId",
                table: "ApplicationRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_PaymentRequests_ApplicationRequests_ApplicationRequestId",
                table: "PaymentRequests");

            migrationBuilder.DropTable(
                name: "PaymentProofs");

            migrationBuilder.DropIndex(
                name: "IX_PaymentRequests_ApplicationRequestId",
                table: "PaymentRequests");

            migrationBuilder.DropIndex(
                name: "IX_PaymentRequests_IdempotencyKey",
                table: "PaymentRequests");

            migrationBuilder.DropIndex(
                name: "IX_ApplicationRequests_PlanId",
                table: "ApplicationRequests");

            migrationBuilder.DropColumn(
                name: "ApplicationRequestId",
                table: "PaymentRequests");

            migrationBuilder.DropColumn(
                name: "BillingCycle",
                table: "PaymentRequests");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "PaymentRequests");

            migrationBuilder.DropColumn(
                name: "IdentityAccountId",
                table: "PaymentRequests");

            migrationBuilder.DropColumn(
                name: "PlanSnapshotJson",
                table: "PaymentRequests");

            migrationBuilder.DropColumn(
                name: "BillingCycle",
                table: "ApplicationRequests");

            migrationBuilder.DropColumn(
                name: "PlanId",
                table: "ApplicationRequests");

            migrationBuilder.DropColumn(
                name: "PlanSnapshotAtUtc",
                table: "ApplicationRequests");

            migrationBuilder.DropColumn(
                name: "PlanSnapshotJson",
                table: "ApplicationRequests");
        }
    }
}
