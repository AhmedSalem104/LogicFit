using LogicFit.Application.Features.DietPlans.Commands.CreateDietPlan;
using LogicFit.Application.Features.WorkoutPrograms.Commands.CreateWorkoutProgram;
using LogicFit.Domain.Enums;
using Xunit;

namespace LogicFit.Tests;

public sealed class CoachPlanFlowContractTests
{
    [Fact]
    public void Active_workout_program_requires_a_complete_nested_aggregate()
    {
        var result = new CreateWorkoutProgramCommandValidator().Validate(new CreateWorkoutProgramCommand
        {
            ClientId = Guid.NewGuid(),
            Name = "Strength",
            StartDate = DateTime.UtcNow.Date,
            Routines = []
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == "Routines");
    }

    [Fact]
    public void Active_diet_plan_requires_at_least_one_meal_but_draft_can_be_saved()
    {
        var active = new CreateDietPlanCommand
        {
            ClientId = Guid.NewGuid(),
            Name = "Cut",
            StartDate = DateTime.UtcNow.Date,
            TargetCalories = 2200,
            TargetProtein = 160,
            TargetCarbs = 220,
            TargetFats = 70
        };
        var draft = new CreateDietPlanCommand
        {
            ClientId = active.ClientId,
            Name = active.Name,
            StartDate = active.StartDate,
            TargetCalories = active.TargetCalories,
            TargetProtein = active.TargetProtein,
            TargetCarbs = active.TargetCarbs,
            TargetFats = active.TargetFats,
            Status = PlanStatus.Draft
        };

        Assert.False(new CreateDietPlanCommandValidator().Validate(active).IsValid);
        Assert.DoesNotContain(new CreateDietPlanCommandValidator().Validate(draft).Errors,
            error => error.PropertyName == "Meals");
    }

    [Fact]
    public void Invalid_nested_food_and_exercise_values_are_rejected_at_the_api_boundary()
    {
        var diet = new CreateDietPlanCommand
        {
            ClientId = Guid.NewGuid(),
            Name = "Plan",
            StartDate = DateTime.UtcNow.Date,
            TargetCalories = 2000,
            Meals =
            [
                new()
                {
                    Name = "Breakfast",
                    Items = [new() { FoodId = 0, AssignedQuantity = 0 }]
                }
            ]
        };
        var workout = new CreateWorkoutProgramCommand
        {
            ClientId = Guid.NewGuid(),
            Name = "Plan",
            StartDate = DateTime.UtcNow.Date,
            Routines =
            [
                new()
                {
                    Name = "Day 1",
                    DayOfWeek = 0,
                    Exercises = [new() { ExerciseId = 0, Sets = 0, RepsMin = 10, RepsMax = 5, RestSec = -1 }]
                }
            ]
        };

        Assert.False(new CreateDietPlanCommandValidator().Validate(diet).IsValid);
        Assert.False(new CreateWorkoutProgramCommandValidator().Validate(workout).IsValid);
    }

    [Fact]
    public void Aggregate_handlers_keep_authorization_and_transaction_boundaries()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var sourceFiles = new[]
        {
            Path.Combine(root, "LogicFit.Application", "Features", "WorkoutPrograms", "Commands", "CreateWorkoutProgram", "CreateWorkoutProgramCommandHandler.cs"),
            Path.Combine(root, "LogicFit.Application", "Features", "WorkoutPrograms", "Commands", "UpdateWorkoutProgram", "UpdateWorkoutProgramCommandHandler.cs"),
            Path.Combine(root, "LogicFit.Application", "Features", "DietPlans", "Commands", "CreateDietPlan", "CreateDietPlanCommandHandler.cs"),
            Path.Combine(root, "LogicFit.Application", "Features", "DietPlans", "Commands", "UpdateDietPlan", "UpdateDietPlanCommandHandler.cs")
        };

        foreach (var path in sourceFiles)
        {
            var source = File.ReadAllText(path);
            Assert.Contains("_accessService", source, StringComparison.Ordinal);
            Assert.Contains("BeginTransactionAsync", source, StringComparison.Ordinal);
            Assert.Contains("SaveChangesAsync", source, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Coach_access_service_is_registered_and_migration_defaults_existing_programs_to_active()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var dependencyInjection = File.ReadAllText(Path.Combine(root, "LogicFit.Application", "DependencyInjection.cs"));
        var migration = File.ReadAllText(Path.Combine(root, "LogicFit.Infrastructure", "Persistence", "Migrations", "20260810125711_CoachPlanExecutionFields.cs"));

        Assert.Contains("ICoachPlanAccessService", dependencyInjection, StringComparison.Ordinal);
        Assert.Contains("name: \"Status\"", migration, StringComparison.Ordinal);
        Assert.Contains("defaultValue: 1", migration, StringComparison.Ordinal);
        Assert.Contains("name: \"TargetWeightKg\"", migration, StringComparison.Ordinal);
        Assert.Contains("name: \"MealsPerDay\"", migration, StringComparison.Ordinal);
    }
}
