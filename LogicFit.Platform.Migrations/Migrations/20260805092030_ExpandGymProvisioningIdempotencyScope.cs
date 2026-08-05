using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogicFit.Platform.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class ExpandGymProvisioningIdempotencyScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ApplicationRequests_IdentityAccountId_TargetScopeKey_ApplicationType",
                table: "ApplicationRequests");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationRequests_IdentityAccountId_TargetScopeKey_ApplicationType",
                table: "ApplicationRequests",
                columns: new[] { "IdentityAccountId", "TargetScopeKey", "ApplicationType" },
                unique: true,
                filter: "[Status] IN (1, 2, 3, 4, 5)");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationRequests_TargetScopeKey_ApplicationType",
                table: "ApplicationRequests",
                columns: new[] { "TargetScopeKey", "ApplicationType" },
                unique: true,
                filter: "[ApplicationType] = 1 AND [Status] IN (1, 2, 3, 4, 5)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ApplicationRequests_IdentityAccountId_TargetScopeKey_ApplicationType",
                table: "ApplicationRequests");

            migrationBuilder.DropIndex(
                name: "IX_ApplicationRequests_TargetScopeKey_ApplicationType",
                table: "ApplicationRequests");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationRequests_IdentityAccountId_TargetScopeKey_ApplicationType",
                table: "ApplicationRequests",
                columns: new[] { "IdentityAccountId", "TargetScopeKey", "ApplicationType" },
                unique: true,
                filter: "[Status] IN (1, 2, 3, 4)");
        }
    }
}
