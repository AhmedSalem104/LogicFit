using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogicFit.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DatabaseResourceConnectionDiagnostics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LastConnectionErrorCode",
                table: "DatabaseResources",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastConnectionErrorMessage",
                table: "DatabaseResources",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastConnectionTestAtUtc",
                table: "DatabaseResources",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LastConnectionTestDurationMs",
                table: "DatabaseResources",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "LastConnectionTestSucceeded",
                table: "DatabaseResources",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ServerHost",
                table: "DatabaseResources",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ServerPort",
                table: "DatabaseResources",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastConnectionErrorCode",
                table: "DatabaseResources");

            migrationBuilder.DropColumn(
                name: "LastConnectionErrorMessage",
                table: "DatabaseResources");

            migrationBuilder.DropColumn(
                name: "LastConnectionTestAtUtc",
                table: "DatabaseResources");

            migrationBuilder.DropColumn(
                name: "LastConnectionTestDurationMs",
                table: "DatabaseResources");

            migrationBuilder.DropColumn(
                name: "LastConnectionTestSucceeded",
                table: "DatabaseResources");

            migrationBuilder.DropColumn(
                name: "ServerHost",
                table: "DatabaseResources");

            migrationBuilder.DropColumn(
                name: "ServerPort",
                table: "DatabaseResources");
        }
    }
}
