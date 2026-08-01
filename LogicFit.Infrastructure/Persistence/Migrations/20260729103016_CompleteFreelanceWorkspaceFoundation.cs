using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogicFit.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CompleteFreelanceWorkspaceFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProvisionedWorkspaceId",
                table: "ApplicationRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestedFieldsJson",
                table: "ApplicationRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "IdentityWorkspaceSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdentityAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByIp = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdentityWorkspaceSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IdentityWorkspaceSessions_IdentityAccounts_IdentityAccountId",
                        column: x => x.IdentityAccountId,
                        principalTable: "IdentityAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationRequests_ProvisionedWorkspaceId",
                table: "ApplicationRequests",
                column: "ProvisionedWorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_IdentityWorkspaceSessions_IdentityAccountId_ExpiresAt",
                table: "IdentityWorkspaceSessions",
                columns: new[] { "IdentityAccountId", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_IdentityWorkspaceSessions_TokenHash",
                table: "IdentityWorkspaceSessions",
                column: "TokenHash",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationRequests_Tenants_ProvisionedWorkspaceId",
                table: "ApplicationRequests",
                column: "ProvisionedWorkspaceId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkspaceMemberships_Tenants_TenantId",
                table: "WorkspaceMemberships",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApplicationRequests_Tenants_ProvisionedWorkspaceId",
                table: "ApplicationRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkspaceMemberships_Tenants_TenantId",
                table: "WorkspaceMemberships");

            migrationBuilder.DropTable(
                name: "IdentityWorkspaceSessions");

            migrationBuilder.DropIndex(
                name: "IX_ApplicationRequests_ProvisionedWorkspaceId",
                table: "ApplicationRequests");

            migrationBuilder.DropColumn(
                name: "ProvisionedWorkspaceId",
                table: "ApplicationRequests");

            migrationBuilder.DropColumn(
                name: "RequestedFieldsJson",
                table: "ApplicationRequests");
        }
    }
}
