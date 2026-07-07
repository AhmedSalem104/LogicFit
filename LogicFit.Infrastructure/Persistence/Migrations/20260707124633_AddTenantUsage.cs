using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogicFit.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantUsage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TenantUsages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MembersCount = table.Column<int>(type: "int", nullable: false),
                    CoachesCount = table.Column<int>(type: "int", nullable: false),
                    EmployeesCount = table.Column<int>(type: "int", nullable: false),
                    BranchesCount = table.Column<int>(type: "int", nullable: false),
                    StorageUsedMB = table.Column<int>(type: "int", nullable: false),
                    LastCalculatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantUsages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenantUsages_TenantId",
                table: "TenantUsages",
                column: "TenantId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantUsages");
        }
    }
}
