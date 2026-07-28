using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogicFit.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStaffQrToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StaffQrCode",
                table: "DomainUsers",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StaffQrGeneratedAt",
                table: "DomainUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StaffQrRevokedAt",
                table: "DomainUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DomainUsers_TenantId_StaffQrCode",
                table: "DomainUsers",
                columns: new[] { "TenantId", "StaffQrCode" },
                unique: true,
                filter: "[StaffQrCode] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DomainUsers_TenantId_StaffQrCode",
                table: "DomainUsers");

            migrationBuilder.DropColumn(
                name: "StaffQrCode",
                table: "DomainUsers");

            migrationBuilder.DropColumn(
                name: "StaffQrGeneratedAt",
                table: "DomainUsers");

            migrationBuilder.DropColumn(
                name: "StaffQrRevokedAt",
                table: "DomainUsers");
        }
    }
}
