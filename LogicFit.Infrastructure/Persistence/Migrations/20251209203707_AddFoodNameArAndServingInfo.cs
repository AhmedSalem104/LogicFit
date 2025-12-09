using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogicFit.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFoodNameArAndServingInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NameAr",
                table: "Foods",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ServingSize",
                table: "Foods",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ServingUnit",
                table: "Foods",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NameAr",
                table: "Foods");

            migrationBuilder.DropColumn(
                name: "ServingSize",
                table: "Foods");

            migrationBuilder.DropColumn(
                name: "ServingUnit",
                table: "Foods");
        }
    }
}
