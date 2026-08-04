using System.Text.Json;
using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LogicFit.Infrastructure.Persistence;

/// <summary>
/// Seeds the reference catalog into each newly allocated tenant database. Catalog rows are
/// deliberately local to the database so tenant operational queries never depend on the legacy
/// shared database.
/// </summary>
public sealed class TenantReferenceCatalogSeeder(ILogger<TenantReferenceCatalogSeeder> logger)
{
    private readonly string _seedDataPath = ResolveSeedDataPath();

    public async Task SeedAsync(
        TenantDbContext context,
        CancellationToken cancellationToken = default)
    {
        var muscles = await SeedMusclesAsync(context, cancellationToken);
        await SeedExercisesAsync(context, muscles, cancellationToken);
        await SeedFoodsAsync(context, cancellationToken);
    }

    private async Task<IReadOnlyList<MuscleSeedDto>> SeedMusclesAsync(
        TenantDbContext context,
        CancellationToken cancellationToken)
    {
        var seedData = await LoadAsync<MuscleSeedDto>("muscles.json", cancellationToken);
        if (seedData.Count == 0)
            return seedData;

        var existingByName = await context.Muscles
            .IgnoreQueryFilters()
            .ToDictionaryAsync(muscle => muscle.Name, StringComparer.Ordinal, cancellationToken);

        var added = 0;
        var updated = 0;
        foreach (var item in seedData)
        {
            if (existingByName.TryGetValue(item.Name, out var existing))
            {
                existing.NameAr = item.NameAr;
                existing.BodyPart = item.BodyPart;
                existing.Description = item.Description;
                existing.DescriptionAr = item.DescriptionAr;
                existing.Icon = item.Icon;
                if (existing.IsDeleted)
                {
                    existing.IsDeleted = false;
                    existing.DeletedAt = null;
                }

                updated++;
                continue;
            }

            var muscle = new Muscle
            {
                Name = item.Name,
                NameAr = item.NameAr,
                BodyPart = item.BodyPart,
                Description = item.Description,
                DescriptionAr = item.DescriptionAr,
                Icon = item.Icon
            };
            context.Muscles.Add(muscle);
            existingByName[item.Name] = muscle;
            added++;
        }

        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Tenant muscles seeded: {Added} added, {Updated} updated for TenantId {TenantId}.",
            added,
            updated,
            context.TenantId);
        return seedData;
    }

    private async Task SeedExercisesAsync(
        TenantDbContext context,
        IReadOnlyList<MuscleSeedDto> muscleSeedData,
        CancellationToken cancellationToken)
    {
        var seedData = await LoadAsync<ExerciseSeedDto>("exercises.json", cancellationToken);
        if (seedData.Count == 0)
            return;

        var muscleNameToId = await context.Muscles
            .IgnoreQueryFilters()
            .ToDictionaryAsync(muscle => muscle.Name, muscle => muscle.Id, StringComparer.Ordinal, cancellationToken);
        var muscleIdMapping = BuildMuscleIdMapping(muscleSeedData, muscleNameToId);
        var existingByName = (await context.Exercises
                .IgnoreQueryFilters()
                .Include(exercise => exercise.SecondaryMuscles)
                .Where(exercise => exercise.TenantId == null)
                .ToListAsync(cancellationToken))
            .GroupBy(exercise => exercise.Name)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        var added = 0;
        var updated = 0;
        var newExercisesWithSecondary = new List<(Exercise Exercise, List<SecondaryMuscleSeedDto> Secondary)>();

        foreach (var item in seedData)
        {
            var targetMuscleId = muscleIdMapping.GetValueOrDefault(item.TargetMuscleId, 1);
            if (existingByName.TryGetValue(item.Name, out var existing))
            {
                ApplyExerciseValues(existing, item, targetMuscleId);
                existing.TenantId = null;
                if (existing.IsDeleted)
                {
                    existing.IsDeleted = false;
                    existing.DeletedAt = null;
                }

                context.ExerciseSecondaryMuscles.RemoveRange(existing.SecondaryMuscles);
                existing.SecondaryMuscles.Clear();
                AddSecondaryMuscles(existing, item.SecondaryMuscles, muscleIdMapping);
                updated++;
                continue;
            }

            var exercise = new Exercise { TenantId = null };
            ApplyExerciseValues(exercise, item, targetMuscleId);
            context.Exercises.Add(exercise);
            existingByName[item.Name] = exercise;
            if (item.SecondaryMuscles is { Count: > 0 })
                newExercisesWithSecondary.Add((exercise, item.SecondaryMuscles));
            added++;
        }

        await context.SaveChangesAsync(cancellationToken);

        foreach (var (exercise, secondaryMuscles) in newExercisesWithSecondary)
            AddSecondaryMuscles(exercise, secondaryMuscles, muscleIdMapping);

        if (newExercisesWithSecondary.Count > 0)
            await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Tenant exercises seeded: {Added} added, {Updated} updated for TenantId {TenantId}.",
            added,
            updated,
            context.TenantId);
    }

    private async Task SeedFoodsAsync(
        TenantDbContext context,
        CancellationToken cancellationToken)
    {
        var seedData = await LoadAsync<FoodSeedDto>("foods.json", cancellationToken);
        if (seedData.Count == 0)
            return;

        var existingByName = (await context.Foods
                .IgnoreQueryFilters()
                .Where(food => food.TenantId == null)
                .ToListAsync(cancellationToken))
            .GroupBy(food => food.Name)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        var added = 0;
        var updated = 0;
        var restored = 0;
        foreach (var item in seedData)
        {
            if (existingByName.TryGetValue(item.NameEn, out var existing))
            {
                ApplyFoodValues(existing, item);
                existing.TenantId = null;
                if (existing.IsDeleted)
                {
                    existing.IsDeleted = false;
                    existing.DeletedAt = null;
                    restored++;
                }

                updated++;
                continue;
            }

            var food = new Food { TenantId = null };
            ApplyFoodValues(food, item);
            context.Foods.Add(food);
            existingByName[item.NameEn] = food;
            added++;
        }

        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Tenant foods seeded: {Added} added, {Updated} updated ({Restored} restored) for TenantId {TenantId}.",
            added,
            updated,
            restored,
            context.TenantId);
    }

    private static void ApplyExerciseValues(Exercise target, ExerciseSeedDto source, int targetMuscleId)
    {
        target.Name = source.Name;
        target.NameAr = source.NameAr;
        target.Description = source.Description;
        target.DescriptionAr = source.DescriptionAr;
        target.TargetMuscleId = targetMuscleId;
        target.Equipment = source.Equipment;
        target.IsHighImpact = source.IsHighImpact;
        target.Difficulty = source.Difficulty;
        target.Category = source.Category;
        target.MovementPattern = source.MovementPattern;
        target.Mechanic = source.Mechanic;
        target.Force = source.Force;
        target.Instructions = Serialize(source.Instructions);
        target.InstructionsAr = Serialize(source.InstructionsAr);
        target.Tips = Serialize(source.Tips);
        target.TipsAr = Serialize(source.TipsAr);
        target.CommonMistakes = Serialize(source.CommonMistakes);
        target.CommonMistakesAr = Serialize(source.CommonMistakesAr);
        target.RepsRange = source.RepsRange;
        target.SetsRange = source.SetsRange;
        target.RestSeconds = source.RestSeconds;
        target.Tempo = source.Tempo;
        target.Icon = source.Icon;
        target.VideoUrl = source.VideoUrl;
    }

    private static void ApplyFoodValues(Food target, FoodSeedDto source)
    {
        target.Name = source.NameEn;
        target.NameAr = source.NameAr;
        target.Category = source.Category;
        target.CaloriesPer100g = (double)source.Calories;
        target.ProteinPer100g = (double)source.Protein;
        target.CarbsPer100g = (double)source.Carbs;
        target.FatsPer100g = (double)source.Fat;
        target.FiberPer100g = (double?)source.Fiber;
        target.SugarPer100g = source.Sugar.HasValue ? (double?)source.Sugar.Value : null;
        target.SodiumPer100g = source.Sodium.HasValue ? (double?)source.Sodium.Value : null;
        target.ServingSize = (double?)source.ServingSize;
        target.ServingUnit = source.ServingUnit;
        target.IsVerified = true;
    }

    private static void AddSecondaryMuscles(
        Exercise exercise,
        IReadOnlyList<SecondaryMuscleSeedDto>? secondaryMuscles,
        IReadOnlyDictionary<int, int> muscleIdMapping)
    {
        if (secondaryMuscles is not { Count: > 0 })
            return;

        foreach (var secondary in secondaryMuscles)
        {
            exercise.SecondaryMuscles.Add(new ExerciseSecondaryMuscle
            {
                ExerciseId = exercise.Id,
                MuscleId = muscleIdMapping.GetValueOrDefault(secondary.MuscleId, 1),
                ContributionPercent = secondary.ContributionPercent
            });
        }
    }

    private static Dictionary<int, int> BuildMuscleIdMapping(
        IReadOnlyList<MuscleSeedDto> seedData,
        IReadOnlyDictionary<string, int> muscleNameToId)
    {
        var fallback = muscleNameToId.GetValueOrDefault("Chest", 1);
        return seedData
            .Select((muscle, index) => new
            {
                JsonId = index + 1,
                DatabaseId = muscleNameToId.GetValueOrDefault(muscle.Name, fallback)
            })
            .ToDictionary(item => item.JsonId, item => item.DatabaseId);
    }

    private async Task<List<T>> LoadAsync<T>(string fileName, CancellationToken cancellationToken)
    {
        var path = Path.Combine(_seedDataPath, fileName);
        if (!File.Exists(path))
        {
            logger.LogWarning("Tenant seed file not found at {Path}.", path);
            return new List<T>();
        }

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<List<T>>(
                   stream,
                   new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                   cancellationToken)
               ?? new List<T>();
    }

    private static string ResolveSeedDataPath()
    {
        var basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SeedData");
        return Directory.Exists(basePath)
            ? basePath
            : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Persistence", "SeedData");
    }

    private static string? Serialize(IReadOnlyList<string>? value) =>
        value is { Count: > 0 } ? JsonSerializer.Serialize(value) : null;
}
