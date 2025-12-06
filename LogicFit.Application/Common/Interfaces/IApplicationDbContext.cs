using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Tenant> Tenants { get; }
    DbSet<User> Users { get; }
    DbSet<UserProfile> UserProfiles { get; }
    DbSet<NutrientDefinition> NutrientDefinitions { get; }
    DbSet<Food> Foods { get; }
    DbSet<FoodMicronutrient> FoodMicronutrients { get; }
    DbSet<Recipe> Recipes { get; }
    DbSet<RecipeIngredient> RecipeIngredients { get; }
    DbSet<DietPlan> DietPlans { get; }
    DbSet<DailyMeal> DailyMeals { get; }
    DbSet<MealItem> MealItems { get; }
    DbSet<MealLog> MealLogs { get; }
    DbSet<Muscle> Muscles { get; }
    DbSet<Exercise> Exercises { get; }
    DbSet<WorkoutProgram> WorkoutPrograms { get; }
    DbSet<ProgramRoutine> ProgramRoutines { get; }
    DbSet<RoutineExercise> RoutineExercises { get; }
    DbSet<WorkoutSession> WorkoutSessions { get; }
    DbSet<SessionSet> SessionSets { get; }
    DbSet<BodyMeasurement> BodyMeasurements { get; }
    DbSet<SubscriptionPlan> SubscriptionPlans { get; }
    DbSet<ClientSubscription> ClientSubscriptions { get; }
    DbSet<SubscriptionFreeze> SubscriptionFreezes { get; }
    DbSet<CoachClient> CoachClients { get; }
    DbSet<AuditLog> AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
