using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogicFit.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TopGymDailyPasses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DayPassSaleId",
                table: "Payments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DayPassTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_DayPassTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DayPassSales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    VisitorName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    VisitorPhone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    VisitorPhoneNormalized = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    DayPassTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PassTypeCodeSnapshot = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    PassTypeNameSnapshot = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    AmountDue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AmountPaid = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentMethod = table.Column<int>(type: "int", nullable: false),
                    VisitDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    WhatsappOpenedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_DayPassSales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DayPassSales_DayPassTypes_DayPassTypeId",
                        column: x => x.DayPassTypeId,
                        principalTable: "DayPassTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_DayPassSaleId",
                table: "Payments",
                column: "DayPassSaleId",
                unique: true,
                filter: "[DayPassSaleId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DayPassSales_DayPassTypeId",
                table: "DayPassSales",
                column: "DayPassTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_DayPassSales_TenantId_ReferenceNumber",
                table: "DayPassSales",
                columns: new[] { "TenantId", "ReferenceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DayPassSales_TenantId_Status_VisitDate",
                table: "DayPassSales",
                columns: new[] { "TenantId", "Status", "VisitDate" });

            migrationBuilder.CreateIndex(
                name: "IX_DayPassSales_TenantId_VisitDate_Id",
                table: "DayPassSales",
                columns: new[] { "TenantId", "VisitDate", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_DayPassSales_TenantId_VisitorPhoneNormalized_VisitDate",
                table: "DayPassSales",
                columns: new[] { "TenantId", "VisitorPhoneNormalized", "VisitDate" });

            migrationBuilder.CreateIndex(
                name: "IX_DayPassTypes_TenantId_Code",
                table: "DayPassTypes",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DayPassTypes_TenantId_IsActive_SortOrder",
                table: "DayPassTypes",
                columns: new[] { "TenantId", "IsActive", "SortOrder" });

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_DayPassSales_DayPassSaleId",
                table: "Payments",
                column: "DayPassSaleId",
                principalTable: "DayPassSales",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_DayPassSales_DayPassSaleId",
                table: "Payments");

            migrationBuilder.DropTable(
                name: "DayPassSales");

            migrationBuilder.DropTable(
                name: "DayPassTypes");

            migrationBuilder.DropIndex(
                name: "IX_Payments_DayPassSaleId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "DayPassSaleId",
                table: "Payments");
        }
    }
}
