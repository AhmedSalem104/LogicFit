using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogicFit.Tenant.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class TopGymDailyPasses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DaysPerWeek",
                table: "WorkoutPrograms",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "WorkoutPrograms",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Difficulty",
                table: "WorkoutPrograms",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Goal",
                table: "WorkoutPrograms",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "WorkoutPrograms",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "WorkoutPrograms",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "WorkoutPrograms",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "RoutineExercises",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "TargetWeightKg",
                table: "RoutineExercises",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tempo",
                table: "RoutineExercises",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "ProgramRoutines",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DayPassSaleId",
                table: "Payments",
                type: "uniqueidentifier",
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

            migrationBuilder.AddColumn<string>(
                name: "MealNameSnapshot",
                table: "MealLogs",
                type: "nvarchar(200)",
                maxLength: 200,
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
                name: "Description",
                table: "DietPlans",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MealsPerDay",
                table: "DietPlans",
                type: "int",
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

            migrationBuilder.AddColumn<string>(
                name: "Time",
                table: "DailyMeals",
                type: "nvarchar(max)",
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
                name: "IX_AthleteCheckins_ClientId",
                table: "AthleteCheckins",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_AthleteCheckins_TenantId_ClientId_CheckinDate",
                table: "AthleteCheckins",
                columns: new[] { "TenantId", "ClientId", "CheckinDate" },
                unique: true);

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
                name: "AthleteCheckins");

            migrationBuilder.DropTable(
                name: "DayPassSales");

            migrationBuilder.DropTable(
                name: "DayPassTypes");

            migrationBuilder.DropIndex(
                name: "IX_Payments_DayPassSaleId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "DaysPerWeek",
                table: "WorkoutPrograms");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "WorkoutPrograms");

            migrationBuilder.DropColumn(
                name: "Difficulty",
                table: "WorkoutPrograms");

            migrationBuilder.DropColumn(
                name: "Goal",
                table: "WorkoutPrograms");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "WorkoutPrograms");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "WorkoutPrograms");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "WorkoutPrograms");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "RoutineExercises");

            migrationBuilder.DropColumn(
                name: "TargetWeightKg",
                table: "RoutineExercises");

            migrationBuilder.DropColumn(
                name: "Tempo",
                table: "RoutineExercises");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "ProgramRoutines");

            migrationBuilder.DropColumn(
                name: "DayPassSaleId",
                table: "Payments");

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
                name: "MealNameSnapshot",
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
                name: "Description",
                table: "DietPlans");

            migrationBuilder.DropColumn(
                name: "MealsPerDay",
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
                name: "Time",
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
