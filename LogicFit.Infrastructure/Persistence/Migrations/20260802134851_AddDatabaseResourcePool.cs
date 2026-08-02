using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogicFit.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDatabaseResourcePool : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DatabaseResources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DatabaseName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ServerKey = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EncryptedConnectionString = table.Column<string>(type: "nvarchar(max)", maxLength: 4096, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReservedForTenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReservedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AssignedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastHealthCheckAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    SchemaVersion = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatabaseResources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TenantDatabaseMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DatabaseResourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EncryptedConnectionString = table.Column<string>(type: "nvarchar(max)", maxLength: 4096, nullable: false),
                    SchemaVersion = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    LastValidatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantDatabaseMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantDatabaseMappings_DatabaseResources_DatabaseResourceId",
                        column: x => x.DatabaseResourceId,
                        principalTable: "DatabaseResources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DatabaseResources_Provider_DatabaseName",
                table: "DatabaseResources",
                columns: new[] { "Provider", "DatabaseName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DatabaseResources_ReservedForTenantId",
                table: "DatabaseResources",
                column: "ReservedForTenantId");

            migrationBuilder.CreateIndex(
                name: "IX_DatabaseResources_Status",
                table: "DatabaseResources",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TenantDatabaseMappings_DatabaseResourceId_IsActive",
                table: "TenantDatabaseMappings",
                columns: new[] { "DatabaseResourceId", "IsActive" },
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_TenantDatabaseMappings_TenantId_IsActive",
                table: "TenantDatabaseMappings",
                columns: new[] { "TenantId", "IsActive" },
                unique: true,
                filter: "[IsActive] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantDatabaseMappings");

            migrationBuilder.DropTable(
                name: "DatabaseResources");
        }
    }
}
