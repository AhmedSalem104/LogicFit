using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogicFit.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TopGymMemberTrainingNutritionParity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "WorkoutPrograms",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "WorkoutPrograms",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "ProgramRoutines",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "FoodCaloriesSnapshot",
                table: "MealLogs",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "FoodCarbsSnapshot",
                table: "MealLogs",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "FoodFatsSnapshot",
                table: "MealLogs",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FoodNameSnapshot",
                table: "MealLogs",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "FoodProteinSnapshot",
                table: "MealLogs",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "FoodServingSizeSnapshot",
                table: "MealLogs",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FoodUnitSnapshot",
                table: "MealLogs",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "FoodServingSizeSnapshot",
                table: "MealItems",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "MealItems",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ServingUnit",
                table: "MealItems",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CalculatorMetadata",
                table: "DietPlans",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "CalorieAdjustment",
                table: "DietPlans",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CalorieGoal",
                table: "DietPlans",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "DietPlans",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "DietPlans",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "DailyMeals",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ArmsCm",
                table: "BodyMeasurements",
                type: "float(10)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ChestCm",
                table: "BodyMeasurements",
                type: "float(10)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "HeightCm",
                table: "BodyMeasurements",
                type: "float(10)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "HipsCm",
                table: "BodyMeasurements",
                type: "float(10)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "BodyMeasurements",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ThighsCm",
                table: "BodyMeasurements",
                type: "float(10)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "WaistCm",
                table: "BodyMeasurements",
                type: "float(10)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AthleteCheckins",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CheckinDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SleepHours = table.Column<double>(type: "float(4)", precision: 4, scale: 1, nullable: true),
                    SleepQuality = table.Column<int>(type: "int", nullable: true),
                    Fatigue = table.Column<int>(type: "int", nullable: true),
                    Soreness = table.Column<int>(type: "int", nullable: true),
                    Stress = table.Column<int>(type: "int", nullable: true),
                    Mood = table.Column<int>(type: "int", nullable: true),
                    RestingHeartRate = table.Column<int>(type: "int", nullable: true),
                    Hrv = table.Column<double>(type: "float(8)", precision: 8, scale: 2, nullable: true),
                    BodyweightKg = table.Column<double>(type: "float(10)", precision: 10, scale: 2, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_AthleteCheckins", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AthleteCheckins_DomainUsers_ClientId",
                        column: x => x.ClientId,
                        principalTable: "DomainUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AthleteCheckins_ClientId",
                table: "AthleteCheckins",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_AthleteCheckins_TenantId_ClientId_CheckinDate",
                table: "AthleteCheckins",
                columns: new[] { "TenantId", "ClientId", "CheckinDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AthleteCheckins");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "WorkoutPrograms");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "WorkoutPrograms");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "ProgramRoutines");

            migrationBuilder.DropColumn(
                name: "FoodCaloriesSnapshot",
                table: "MealLogs");

            migrationBuilder.DropColumn(
                name: "FoodCarbsSnapshot",
                table: "MealLogs");

            migrationBuilder.DropColumn(
                name: "FoodFatsSnapshot",
                table: "MealLogs");

            migrationBuilder.DropColumn(
                name: "FoodNameSnapshot",
                table: "MealLogs");

            migrationBuilder.DropColumn(
                name: "FoodProteinSnapshot",
                table: "MealLogs");

            migrationBuilder.DropColumn(
                name: "FoodServingSizeSnapshot",
                table: "MealLogs");

            migrationBuilder.DropColumn(
                name: "FoodUnitSnapshot",
                table: "MealLogs");

            migrationBuilder.DropColumn(
                name: "FoodServingSizeSnapshot",
                table: "MealItems");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "MealItems");

            migrationBuilder.DropColumn(
                name: "ServingUnit",
                table: "MealItems");

            migrationBuilder.DropColumn(
                name: "CalculatorMetadata",
                table: "DietPlans");

            migrationBuilder.DropColumn(
                name: "CalorieAdjustment",
                table: "DietPlans");

            migrationBuilder.DropColumn(
                name: "CalorieGoal",
                table: "DietPlans");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "DietPlans");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "DietPlans");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "DailyMeals");

            migrationBuilder.DropColumn(
                name: "ArmsCm",
                table: "BodyMeasurements");

            migrationBuilder.DropColumn(
                name: "ChestCm",
                table: "BodyMeasurements");

            migrationBuilder.DropColumn(
                name: "HeightCm",
                table: "BodyMeasurements");

            migrationBuilder.DropColumn(
                name: "HipsCm",
                table: "BodyMeasurements");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "BodyMeasurements");

            migrationBuilder.DropColumn(
                name: "ThighsCm",
                table: "BodyMeasurements");

            migrationBuilder.DropColumn(
                name: "WaistCm",
                table: "BodyMeasurements");
        }
    }
}
