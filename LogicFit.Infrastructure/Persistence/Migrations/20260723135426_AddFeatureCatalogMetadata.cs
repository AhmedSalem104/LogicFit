using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogicFit.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFeatureCatalogMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EndsAt",
                table: "TenantFeatures",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "GrantedByUserId",
                table: "TenantFeatures",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "TenantFeatures",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "StartsAt",
                table: "TenantFeatures",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsFree",
                table: "Features",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Module",
                table: "Features",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NameAr",
                table: "Features",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NameEn",
                table: "Features",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Features",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "SupportsQuota",
                table: "Features",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "FeatureDependencies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FeatureId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DependsOnFeatureId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureDependencies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeatureDependencies_Features_DependsOnFeatureId",
                        column: x => x.DependsOnFeatureId,
                        principalTable: "Features",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FeatureDependencies_Features_FeatureId",
                        column: x => x.FeatureId,
                        principalTable: "Features",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FeatureQuotaDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FeatureId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResourceKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DefaultLimit = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureQuotaDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeatureQuotaDefinitions_Features_FeatureId",
                        column: x => x.FeatureId,
                        principalTable: "Features",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FeatureDependencies_DependsOnFeatureId",
                table: "FeatureDependencies",
                column: "DependsOnFeatureId");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureDependencies_FeatureId_DependsOnFeatureId",
                table: "FeatureDependencies",
                columns: new[] { "FeatureId", "DependsOnFeatureId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FeatureQuotaDefinitions_FeatureId_ResourceKey",
                table: "FeatureQuotaDefinitions",
                columns: new[] { "FeatureId", "ResourceKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FeatureDependencies");

            migrationBuilder.DropTable(
                name: "FeatureQuotaDefinitions");

            migrationBuilder.DropColumn(
                name: "EndsAt",
                table: "TenantFeatures");

            migrationBuilder.DropColumn(
                name: "GrantedByUserId",
                table: "TenantFeatures");

            migrationBuilder.DropColumn(
                name: "Reason",
                table: "TenantFeatures");

            migrationBuilder.DropColumn(
                name: "StartsAt",
                table: "TenantFeatures");

            migrationBuilder.DropColumn(
                name: "IsFree",
                table: "Features");

            migrationBuilder.DropColumn(
                name: "Module",
                table: "Features");

            migrationBuilder.DropColumn(
                name: "NameAr",
                table: "Features");

            migrationBuilder.DropColumn(
                name: "NameEn",
                table: "Features");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Features");

            migrationBuilder.DropColumn(
                name: "SupportsQuota",
                table: "Features");
        }
    }
}
