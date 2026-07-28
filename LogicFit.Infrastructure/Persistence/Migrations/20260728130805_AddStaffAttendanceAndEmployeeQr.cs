using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogicFit.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStaffAttendanceAndEmployeeQr : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "StaffUserId",
                table: "GateAccessLogs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubjectType",
                table: "GateAccessLogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QrCode",
                table: "EmployeeProfiles",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "QrGeneratedAt",
                table: "EmployeeProfiles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "QrRevokedAt",
                table: "EmployeeProfiles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StaffAttendances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CheckInTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CheckOutTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Method = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffAttendances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StaffAttendances_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StaffAttendances_DomainUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "DomainUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StaffAttendances_EmployeeProfiles_EmployeeProfileId",
                        column: x => x.EmployeeProfileId,
                        principalTable: "EmployeeProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeProfiles_TenantId_QrCode",
                table: "EmployeeProfiles",
                columns: new[] { "TenantId", "QrCode" },
                unique: true,
                filter: "[QrCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StaffAttendances_BranchId",
                table: "StaffAttendances",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffAttendances_EmployeeProfileId",
                table: "StaffAttendances",
                column: "EmployeeProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffAttendances_TenantId_BranchId",
                table: "StaffAttendances",
                columns: new[] { "TenantId", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "IX_StaffAttendances_TenantId_UserId_CheckInTime",
                table: "StaffAttendances",
                columns: new[] { "TenantId", "UserId", "CheckInTime" });

            migrationBuilder.CreateIndex(
                name: "IX_StaffAttendances_UserId",
                table: "StaffAttendances",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StaffAttendances");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeProfiles_TenantId_QrCode",
                table: "EmployeeProfiles");

            migrationBuilder.DropColumn(
                name: "StaffUserId",
                table: "GateAccessLogs");

            migrationBuilder.DropColumn(
                name: "SubjectType",
                table: "GateAccessLogs");

            migrationBuilder.DropColumn(
                name: "QrCode",
                table: "EmployeeProfiles");

            migrationBuilder.DropColumn(
                name: "QrGeneratedAt",
                table: "EmployeeProfiles");

            migrationBuilder.DropColumn(
                name: "QrRevokedAt",
                table: "EmployeeProfiles");
        }
    }
}
