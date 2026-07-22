using System.Text.Json;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LogicFit.Infrastructure.Persistence;

public class DataSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DataSeeder> _logger;
    private readonly RbacSeeder _rbacSeeder;
    private readonly PlanSeeder _planSeeder;
    private readonly string _seedDataPath;

    public DataSeeder(ApplicationDbContext context, ILogger<DataSeeder> logger, RbacSeeder rbacSeeder, PlanSeeder planSeeder)
    {
        _context = context;
        _logger = logger;
        _rbacSeeder = rbacSeeder;
        _planSeeder = planSeeder;
        // Check multiple possible locations for seed data
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        _seedDataPath = Path.Combine(baseDir, "SeedData");

        if (!Directory.Exists(_seedDataPath))
        {
            _seedDataPath = Path.Combine(baseDir, "Persistence", "SeedData");
        }

        _logger.LogInformation("Seed data path: {Path}", _seedDataPath);
    }

    public async Task SeedAsync()
    {
        try
        {
            await SeedTenantsAsync();
            await SeedMusclesAsync();
            await SeedExercisesAsync();
            await SeedFoodsAsync();
            await SeedUsersAsync();
            await _rbacSeeder.SeedAsync();
            await _planSeeder.SeedAsync();

            _logger.LogInformation("Data seeding completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }
    }

    /// <summary>
    /// Force reset foods with identity reseed. Use this to fix the database if food IDs
    /// have become out of sync due to previous delete/reseed cycles.
    /// WARNING: This will delete all MealItems, RecipeIngredients, and FoodMicronutrients!
    /// </summary>
    public async Task ForceResetFoodsAsync()
    {
        _logger.LogWarning("Starting force reset of Foods table...");

        // This is an explicit destructive maintenance operation. Use hard deletes here because
        // EF's SaveChanges converts RemoveRange on soft-deletable entities into updates; reseeding
        // identity while those rows remain would cause primary-key collisions during reseeding.
        await using var transaction = await _context.Database.BeginTransactionAsync();
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM MealItems");
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM RecipeIngredients");
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM FoodMicronutrients");
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM Foods");
        await _context.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('Foods', RESEED, 0)");
        await transaction.CommitAsync();
        _logger.LogInformation("Reset Foods identity seed to 0");

        // Now seed fresh
        await SeedFoodsAsync();
        _logger.LogWarning("Force reset of Foods completed. New foods seeded with IDs starting from 1.");
    }

    private async Task SeedTenantsAsync()
    {
        if (await _context.Tenants.AnyAsync())
        {
            _logger.LogInformation("Tenants already seeded, skipping...");
            return;
        }

        var jsonPath = Path.Combine(_seedDataPath, "tenants.json");
        if (!File.Exists(jsonPath))
        {
            _logger.LogWarning("tenants.json not found at {Path}", jsonPath);
            return;
        }

        var json = await File.ReadAllTextAsync(jsonPath);
        var seedData = JsonSerializer.Deserialize<List<TenantSeedDto>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (seedData == null || !seedData.Any()) return;

        foreach (var item in seedData)
        {
            var tenant = new Tenant
            {
                Id = item.Id,
                Name = item.Name,
                Subdomain = item.Subdomain,
                Status = (TenantStatus)item.Status,
                BrandingSettings = item.BrandingSettings != null ? new BrandingSettings
                {
                    LogoUrl = item.BrandingSettings.LogoUrl,
                    PrimaryColor = item.BrandingSettings.PrimaryColor,
                    SecondaryColor = item.BrandingSettings.SecondaryColor
                } : null
            };
            _context.Tenants.Add(tenant);
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} tenants", seedData.Count);
    }

    private async Task SeedMusclesAsync()
    {
        var jsonPath = Path.Combine(_seedDataPath, "muscles.json");
        if (!File.Exists(jsonPath))
        {
            _logger.LogWarning("muscles.json not found at {Path}", jsonPath);
            return;
        }

        var json = await File.ReadAllTextAsync(jsonPath);
        var seedData = JsonSerializer.Deserialize<List<MuscleSeedDto>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (seedData == null || !seedData.Any()) return;

        // Get existing muscles for UPSERT
        var existingMuscles = await _context.Muscles
            .IgnoreQueryFilters()
            .ToListAsync();
        var existingByName = existingMuscles.ToDictionary(m => m.Name, m => m);

        int added = 0, updated = 0;

        foreach (var item in seedData)
        {
            if (existingByName.TryGetValue(item.Name, out var existing))
            {
                // UPDATE existing
                existing.NameAr = item.NameAr;
                existing.BodyPart = item.BodyPart;
                existing.Description = item.Description;
                existing.DescriptionAr = item.DescriptionAr;
                existing.Icon = item.Icon;

                // Restore if soft-deleted
                if (existing.IsDeleted)
                {
                    existing.IsDeleted = false;
                    existing.DeletedAt = null;
                }
                updated++;
            }
            else
            {
                // INSERT new
                var muscle = new Muscle
                {
                    Name = item.Name,
                    NameAr = item.NameAr,
                    BodyPart = item.BodyPart,
                    Description = item.Description,
                    DescriptionAr = item.DescriptionAr,
                    Icon = item.Icon
                };
                _context.Muscles.Add(muscle);
                added++;
            }
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Muscles: {Added} added, {Updated} updated", added, updated);
    }

    private async Task SeedExercisesAsync()
    {
        var jsonPath = Path.Combine(_seedDataPath, "exercises.json");
        if (!File.Exists(jsonPath))
        {
            _logger.LogWarning("exercises.json not found at {Path}", jsonPath);
            return;
        }

        var json = await File.ReadAllTextAsync(jsonPath);
        var seedData = JsonSerializer.Deserialize<List<ExerciseSeedDto>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (seedData == null || !seedData.Any()) return;

        // Get muscles with their auto-generated IDs to map exercises correctly
        var muscles = await _context.Muscles.ToListAsync();
        var muscleNameToId = muscles.ToDictionary(m => m.Name, m => m.Id);

        // Build mapping from old JSON muscle IDs to database IDs
        var muscleIdMapping = BuildMuscleIdMapping(muscleNameToId);

        // Get existing global exercises for upsert
        var existingExercises = await _context.Exercises
            .Include(e => e.SecondaryMuscles)
            .Where(e => e.TenantId == null)
            .ToListAsync();
        var existingByName = existingExercises.ToDictionary(e => e.Name, e => e);

        int added = 0, updated = 0;
        var newExercisesWithSecondary = new List<(Exercise exercise, List<SecondaryMuscleSeedDto> secondary)>();

        foreach (var item in seedData)
        {
            // Map muscle ID from JSON to database ID
            var targetMuscleId = muscleIdMapping.GetValueOrDefault(item.TargetMuscleId, 1);

            if (existingByName.TryGetValue(item.Name, out var existing))
            {
                // UPDATE existing
                existing.NameAr = item.NameAr;
                existing.Description = item.Description;
                existing.DescriptionAr = item.DescriptionAr;
                existing.TargetMuscleId = targetMuscleId;
                existing.Equipment = item.Equipment;
                existing.IsHighImpact = item.IsHighImpact;
                existing.Difficulty = item.Difficulty;
                existing.Category = item.Category;
                existing.MovementPattern = item.MovementPattern;
                existing.Mechanic = item.Mechanic;
                existing.Force = item.Force;
                existing.Instructions = item.Instructions != null ? JsonSerializer.Serialize(item.Instructions) : null;
                existing.InstructionsAr = item.InstructionsAr != null ? JsonSerializer.Serialize(item.InstructionsAr) : null;
                existing.Tips = item.Tips != null ? JsonSerializer.Serialize(item.Tips) : null;
                existing.TipsAr = item.TipsAr != null ? JsonSerializer.Serialize(item.TipsAr) : null;
                existing.CommonMistakes = item.CommonMistakes != null ? JsonSerializer.Serialize(item.CommonMistakes) : null;
                existing.CommonMistakesAr = item.CommonMistakesAr != null ? JsonSerializer.Serialize(item.CommonMistakesAr) : null;
                existing.RepsRange = item.RepsRange;
                existing.SetsRange = item.SetsRange;
                existing.RestSeconds = item.RestSeconds;
                existing.Tempo = item.Tempo;
                existing.Icon = item.Icon;
                existing.VideoUrl = item.VideoUrl;

                // Update secondary muscles - remove old, add new
                if (existing.SecondaryMuscles.Any())
                {
                    _context.ExerciseSecondaryMuscles.RemoveRange(existing.SecondaryMuscles);
                }

                if (item.SecondaryMuscles != null && item.SecondaryMuscles.Any())
                {
                    foreach (var sm in item.SecondaryMuscles)
                    {
                        var secondaryMuscleId = muscleIdMapping.GetValueOrDefault(sm.MuscleId, 1);
                        existing.SecondaryMuscles.Add(new ExerciseSecondaryMuscle
                        {
                            ExerciseId = existing.Id,
                            MuscleId = secondaryMuscleId,
                            ContributionPercent = sm.ContributionPercent
                        });
                    }
                }
                updated++;
            }
            else
            {
                // INSERT new
                var exercise = new Exercise
                {
                    TenantId = item.TenantId,
                    Name = item.Name,
                    NameAr = item.NameAr,
                    Description = item.Description,
                    DescriptionAr = item.DescriptionAr,
                    TargetMuscleId = targetMuscleId,
                    Equipment = item.Equipment,
                    IsHighImpact = item.IsHighImpact,
                    Difficulty = item.Difficulty,
                    Category = item.Category,
                    MovementPattern = item.MovementPattern,
                    Mechanic = item.Mechanic,
                    Force = item.Force,
                    Instructions = item.Instructions != null ? JsonSerializer.Serialize(item.Instructions) : null,
                    InstructionsAr = item.InstructionsAr != null ? JsonSerializer.Serialize(item.InstructionsAr) : null,
                    Tips = item.Tips != null ? JsonSerializer.Serialize(item.Tips) : null,
                    TipsAr = item.TipsAr != null ? JsonSerializer.Serialize(item.TipsAr) : null,
                    CommonMistakes = item.CommonMistakes != null ? JsonSerializer.Serialize(item.CommonMistakes) : null,
                    CommonMistakesAr = item.CommonMistakesAr != null ? JsonSerializer.Serialize(item.CommonMistakesAr) : null,
                    RepsRange = item.RepsRange,
                    SetsRange = item.SetsRange,
                    RestSeconds = item.RestSeconds,
                    Tempo = item.Tempo,
                    Icon = item.Icon,
                    VideoUrl = item.VideoUrl
                };
                _context.Exercises.Add(exercise);

                // Store for second pass (need ID after SaveChanges)
                if (item.SecondaryMuscles != null && item.SecondaryMuscles.Any())
                {
                    newExercisesWithSecondary.Add((exercise, item.SecondaryMuscles));
                }
                added++;
            }
        }

        await _context.SaveChangesAsync();

        // Second pass: Add secondary muscles for new exercises (now they have IDs)
        foreach (var (exercise, secondaryMuscles) in newExercisesWithSecondary)
        {
            foreach (var sm in secondaryMuscles)
            {
                var secondaryMuscleId = muscleIdMapping.GetValueOrDefault(sm.MuscleId, 1);
                _context.ExerciseSecondaryMuscles.Add(new ExerciseSecondaryMuscle
                {
                    ExerciseId = exercise.Id,
                    MuscleId = secondaryMuscleId,
                    ContributionPercent = sm.ContributionPercent
                });
            }
        }

        if (newExercisesWithSecondary.Any())
        {
            await _context.SaveChangesAsync();
        }

        _logger.LogInformation("Exercises: {Added} added, {Updated} updated", added, updated);
    }

    private async Task SeedFoodsAsync()
    {
        var jsonPath = Path.Combine(_seedDataPath, "foods.json");
        if (!File.Exists(jsonPath))
        {
            _logger.LogWarning("foods.json not found at {Path}", jsonPath);
            return;
        }

        var json = await File.ReadAllTextAsync(jsonPath);
        var seedData = JsonSerializer.Deserialize<List<FoodSeedDto>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (seedData == null || !seedData.Any()) return;

        // Get existing global foods for UPSERT (including soft-deleted ones)
        var existingGlobalFoods = await _context.Foods
            .IgnoreQueryFilters()
            .Where(f => f.TenantId == null)
            .ToListAsync();

        // Create lookup by name for efficient matching (handle duplicates by taking first)
        var existingByName = existingGlobalFoods
            .GroupBy(f => f.Name)
            .ToDictionary(g => g.Key, g => g.First());

        int added = 0, updated = 0, restored = 0;

        foreach (var item in seedData)
        {
            if (existingByName.TryGetValue(item.NameEn, out var existing))
            {
                // UPDATE existing food (preserve ID)
                existing.NameAr = item.NameAr;
                existing.Category = item.Category;
                existing.CaloriesPer100g = (double)item.Calories;
                existing.ProteinPer100g = (double)item.Protein;
                existing.CarbsPer100g = (double)item.Carbs;
                existing.FatsPer100g = (double)item.Fat;
                existing.FiberPer100g = (double?)item.Fiber;
                existing.SugarPer100g = item.Sugar.HasValue ? (double?)item.Sugar.Value : null;
                existing.SodiumPer100g = item.Sodium.HasValue ? (double?)item.Sodium.Value : null;
                existing.ServingSize = (double?)item.ServingSize;
                existing.ServingUnit = item.ServingUnit;
                existing.IsVerified = true;

                // Restore if soft-deleted
                if (existing.IsDeleted)
                {
                    existing.IsDeleted = false;
                    existing.DeletedAt = null;
                    restored++;
                }
                updated++;
            }
            else
            {
                // INSERT new food
                var food = new Food
                {
                    TenantId = item.TenantId,
                    Name = item.NameEn,
                    NameAr = item.NameAr,
                    Category = item.Category,
                    CaloriesPer100g = (double)item.Calories,
                    ProteinPer100g = (double)item.Protein,
                    CarbsPer100g = (double)item.Carbs,
                    FatsPer100g = (double)item.Fat,
                    FiberPer100g = (double?)item.Fiber,
                    SugarPer100g = item.Sugar.HasValue ? (double?)item.Sugar.Value : null,
                    SodiumPer100g = item.Sodium.HasValue ? (double?)item.Sodium.Value : null,
                    ServingSize = (double?)item.ServingSize,
                    ServingUnit = item.ServingUnit,
                    IsVerified = true
                };
                _context.Foods.Add(food);
                added++;
            }
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Foods: {Added} added, {Updated} updated ({Restored} restored from deleted)", added, updated, restored);
    }

    private async Task SeedUsersAsync()
    {
        if (await _context.Set<User>().AnyAsync())
        {
            _logger.LogInformation("Users already seeded, skipping...");
            return;
        }

        var jsonPath = Path.Combine(_seedDataPath, "users.json");
        if (!File.Exists(jsonPath))
        {
            _logger.LogWarning("users.json not found at {Path}", jsonPath);
            return;
        }

        var json = await File.ReadAllTextAsync(jsonPath);
        var seedData = JsonSerializer.Deserialize<List<UserSeedDto>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (seedData == null || !seedData.Any()) return;

        foreach (var item in seedData)
        {
            var user = new User
            {
                Id = item.Id,
                TenantId = item.TenantId,
                Email = item.Email,
                PhoneNumber = item.PhoneNumber,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(item.Password),
                Role = (UserRole)item.Role,
                IsActive = item.IsActive,
                WalletBalance = item.WalletBalance
            };
            _context.Set<User>().Add(user);

            // Create user profile
            if (!string.IsNullOrEmpty(item.FullName))
            {
                var profile = new UserProfile
                {
                    UserId = user.Id,
                    FullName = item.FullName
                };
                _context.UserProfiles.Add(profile);
            }
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} users", seedData.Count);
    }

    /// <summary>
    /// Build mapping from JSON muscle IDs to database muscle IDs.
    /// The JSON uses sequential IDs (1, 2, 3...) based on muscle name order.
    /// This maps those to the actual database IDs after muscles are seeded.
    /// </summary>
    private Dictionary<int, int> BuildMuscleIdMapping(Dictionary<string, int> muscleNameToDbId)
    {
        // Map from JSON ID to muscle name (based on the order in muscles.json)
        // The JSON IDs correspond to these muscle names in order
        var jsonIdToName = new Dictionary<int, string>
        {
            { 1, "Chest" }, { 2, "Upper Chest" }, { 3, "Lower Chest" }, { 4, "Inner Chest" }, { 5, "Outer Chest" },
            { 6, "Pectoralis Minor" }, { 7, "Back" }, { 8, "Upper Back" }, { 9, "Middle Back" }, { 10, "Lats" },
            { 11, "Rhomboids" }, { 12, "Rhomboid Major" }, { 13, "Rhomboid Minor" }, { 14, "Teres Major" }, { 15, "Teres Minor" },
            { 16, "Serratus Anterior" }, { 17, "Traps" }, { 18, "Upper Traps" }, { 19, "Middle Traps" }, { 20, "Lower Traps" },
            { 21, "Shoulders" }, { 22, "Front Deltoid" }, { 23, "Side Deltoid" }, { 24, "Rear Deltoid" }, { 25, "Rotator Cuff" },
            { 26, "Infraspinatus" }, { 27, "Supraspinatus" }, { 28, "Subscapularis" }, { 29, "Biceps" }, { 30, "Long Head Biceps" },
            { 31, "Short Head Biceps" }, { 32, "Brachialis" }, { 33, "Brachioradialis" }, { 34, "Coracobrachialis" }, { 35, "Triceps" },
            { 36, "Long Head Triceps" }, { 37, "Lateral Head Triceps" }, { 38, "Medial Head Triceps" }, { 39, "Anconeus" }, { 40, "Forearms" },
            { 41, "Wrist Flexors" }, { 42, "Wrist Extensors" }, { 43, "Pronator Teres" }, { 44, "Supinator" }, { 45, "Finger Flexors" },
            { 46, "Finger Extensors" }, { 47, "Grip Muscles" }, { 48, "Thenar Muscles" }, { 49, "Hypothenar Muscles" }, { 50, "Abs" },
            { 51, "Upper Abs" }, { 52, "Lower Abs" }, { 53, "Rectus Abdominis" }, { 54, "Obliques" }, { 55, "External Obliques" },
            { 56, "Internal Obliques" }, { 57, "Transverse Abdominis" }, { 58, "Lower Back" }, { 59, "Erector Spinae" }, { 60, "Spinalis" },
            { 61, "Longissimus" }, { 62, "Iliocostalis" }, { 63, "Quadratus Lumborum" }, { 64, "Multifidus" }, { 65, "Psoas Major" },
            { 66, "Iliacus" }, { 67, "Hip Flexors" }, { 68, "Pelvic Floor" }, { 69, "Diaphragm" }, { 70, "Intercostals" },
            { 71, "External Intercostals" }, { 72, "Internal Intercostals" }, { 73, "Quadriceps" }, { 74, "Rectus Femoris" }, { 75, "Vastus Lateralis" },
            { 76, "Vastus Medialis" }, { 77, "Vastus Intermedius" }, { 78, "Hamstrings" }, { 79, "Biceps Femoris" }, { 80, "Semitendinosus" },
            { 81, "Semimembranosus" }, { 82, "Glutes" }, { 83, "Gluteus Maximus" }, { 84, "Gluteus Medius" }, { 85, "Gluteus Minimus" },
            { 86, "Piriformis" }, { 87, "Tensor Fasciae Latae" }, { 88, "Adductors" }, { 89, "Adductor Magnus" }, { 90, "Adductor Longus" },
            { 91, "Adductor Brevis" }, { 92, "Pectineus" }, { 93, "Gracilis" }, { 94, "Abductors" }, { 95, "Sartorius" },
            { 96, "Calves" }, { 97, "Gastrocnemius" }, { 98, "Soleus" }, { 99, "Plantaris" }, { 100, "Tibialis Anterior" },
            { 101, "Tibialis Posterior" }, { 102, "Peroneus Longus" }, { 103, "Peroneus Brevis" }, { 104, "Peroneus Tertius" }, { 105, "Popliteus" },
            { 106, "Foot Intrinsics" }, { 107, "Toe Flexors" }, { 108, "Toe Extensors" }, { 109, "Extensor Digitorum Longus" }, { 110, "Flexor Digitorum Longus" },
            { 111, "Flexor Hallucis Longus" }, { 112, "Neck" }, { 113, "Sternocleidomastoid" }, { 114, "Scalenes" }, { 115, "Levator Scapulae" },
            { 116, "Splenius Capitis" }, { 117, "Splenius Cervicis" }, { 118, "Longus Colli" }, { 119, "Longus Capitis" }, { 120, "Platysma" },
            { 121, "Suboccipitals" }, { 122, "Semispinalis Capitis" }, { 123, "Deep Hip Rotators" }, { 124, "Gemellus Superior" }, { 125, "Gemellus Inferior" },
            { 126, "Obturator Internus" }, { 127, "Obturator Externus" }, { 128, "Quadratus Femoris" }, { 129, "Intertransversarii" }, { 130, "Interspinales" },
            { 131, "Rotatores" }, { 132, "Serratus Posterior Superior" }, { 133, "Serratus Posterior Inferior" }, { 134, "Pyramidalis" }, { 135, "Cremaster" }
        };

        // Build the mapping from JSON ID to database ID
        var result = new Dictionary<int, int>();
        foreach (var kvp in jsonIdToName)
        {
            if (muscleNameToDbId.TryGetValue(kvp.Value, out var dbId))
            {
                result[kvp.Key] = dbId;
            }
            else
            {
                // Fallback to Chest (ID 1) if muscle not found
                result[kvp.Key] = muscleNameToDbId.GetValueOrDefault("Chest", 1);
            }
        }

        return result;
    }
}

// DTOs for seed data
public class TenantSeedDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Subdomain { get; set; }
    public int Status { get; set; }
    public BrandingSettingsSeedDto? BrandingSettings { get; set; }
}

public class BrandingSettingsSeedDto
{
    public string? LogoUrl { get; set; }
    public string? PrimaryColor { get; set; }
    public string? SecondaryColor { get; set; }
}

public class MuscleSeedDto
{
    public string Name { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public string? BodyPart { get; set; }
    public string? Description { get; set; }
    public string? DescriptionAr { get; set; }
    public string? Icon { get; set; }
}

public class ExerciseSeedDto
{
    public Guid? TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public string? Description { get; set; }
    public string? DescriptionAr { get; set; }
    public int TargetMuscleId { get; set; }
    public string? Equipment { get; set; }
    public bool IsHighImpact { get; set; }
    public string? Difficulty { get; set; }
    public string? Category { get; set; }
    public string? MovementPattern { get; set; }
    public string? Mechanic { get; set; }
    public string? Force { get; set; }
    public List<string>? Instructions { get; set; }
    public List<string>? InstructionsAr { get; set; }
    public List<string>? Tips { get; set; }
    public List<string>? TipsAr { get; set; }
    public List<string>? CommonMistakes { get; set; }
    public List<string>? CommonMistakesAr { get; set; }
    public string? RepsRange { get; set; }
    public string? SetsRange { get; set; }
    public int? RestSeconds { get; set; }
    public string? Tempo { get; set; }
    public string? Icon { get; set; }
    public string? VideoUrl { get; set; }
    public List<SecondaryMuscleSeedDto>? SecondaryMuscles { get; set; }
}

public class SecondaryMuscleSeedDto
{
    public int MuscleId { get; set; }
    public int ContributionPercent { get; set; }
}

public class FoodSeedDto
{
    public Guid? TenantId { get; set; }
    public string? NameAr { get; set; }
    public string NameEn { get; set; } = string.Empty;
    public string? Category { get; set; }
    public decimal Calories { get; set; }
    public decimal Protein { get; set; }
    public decimal Carbs { get; set; }
    public decimal Fat { get; set; }
    public decimal Fiber { get; set; }
    public decimal? Sugar { get; set; }
    public decimal? Sodium { get; set; }
    public decimal ServingSize { get; set; }
    public string? ServingUnit { get; set; }
}

public class UserSeedDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string Password { get; set; } = string.Empty;
    public int Role { get; set; }
    public bool IsActive { get; set; }
    public string? FullName { get; set; }
    public decimal WalletBalance { get; set; }
}
