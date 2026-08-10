using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogicFit.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CoachPlanExecutionFields : Migration
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

            migrationBuilder.AddColumn<int>(
                name: "Status",
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
                name: "Time",
                table: "DailyMeals",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
                name: "Status",
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
                name: "Description",
                table: "DietPlans");

            migrationBuilder.DropColumn(
                name: "MealsPerDay",
                table: "DietPlans");

            migrationBuilder.DropColumn(
                name: "Time",
                table: "DailyMeals");
        }
    }
}
