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
    private readonly string _seedDataPath;

    public DataSeeder(ApplicationDbContext context, ILogger<DataSeeder> logger)
    {
        _context = context;
        _logger = logger;
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

            _logger.LogInformation("Data seeding completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }
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
                Status = (SubscriptionStatus)item.Status,
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
        if (await _context.Muscles.AnyAsync())
        {
            _logger.LogInformation("Muscles already seeded, skipping...");
            return;
        }

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

        foreach (var item in seedData)
        {
            var muscle = new Muscle
            {
                Name = item.Name,
                BodyPart = item.BodyPart
            };
            _context.Muscles.Add(muscle);
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} muscles", seedData.Count);
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
            // Map muscle name to auto-generated ID
            var targetMuscleId = muscleNameToId.GetValueOrDefault(GetMuscleNameById(item.TargetMuscleId), 1);

            if (existingByName.TryGetValue(item.Name, out var existing))
            {
                // UPDATE existing
                existing.TargetMuscleId = targetMuscleId;
                existing.Equipment = item.Equipment;
                existing.IsHighImpact = item.IsHighImpact;

                // Update secondary muscles - remove old, add new
                if (existing.SecondaryMuscles.Any())
                {
                    _context.ExerciseSecondaryMuscles.RemoveRange(existing.SecondaryMuscles);
                }

                if (item.SecondaryMuscles != null && item.SecondaryMuscles.Any())
                {
                    foreach (var sm in item.SecondaryMuscles)
                    {
                        var secondaryMuscleId = muscleNameToId.GetValueOrDefault(GetMuscleNameById(sm.MuscleId), 1);
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
                    TargetMuscleId = targetMuscleId,
                    Equipment = item.Equipment,
                    IsHighImpact = item.IsHighImpact
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
                var secondaryMuscleId = muscleNameToId.GetValueOrDefault(GetMuscleNameById(sm.MuscleId), 1);
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

        // Get existing global foods for upsert (use first if duplicates exist)
        var existingFoods = await _context.Foods
            .Where(f => f.TenantId == null)
            .ToListAsync();
        var existingByName = existingFoods
            .GroupBy(f => f.Name)
            .ToDictionary(g => g.Key, g => g.First());

        int added = 0, updated = 0;

        foreach (var item in seedData)
        {
            if (existingByName.TryGetValue(item.NameEn, out var existing))
            {
                // UPDATE existing
                existing.Category = item.Category;
                existing.CaloriesPer100g = (double)item.Calories;
                existing.ProteinPer100g = (double)item.Protein;
                existing.CarbsPer100g = (double)item.Carbs;
                existing.FatsPer100g = (double)item.Fat;
                existing.FiberPer100g = (double)item.Fiber;
                updated++;
            }
            else
            {
                // INSERT new
                var food = new Food
                {
                    TenantId = item.TenantId,
                    Name = item.NameEn,
                    Category = item.Category,
                    CaloriesPer100g = (double)item.Calories,
                    ProteinPer100g = (double)item.Protein,
                    CarbsPer100g = (double)item.Carbs,
                    FatsPer100g = (double)item.Fat,
                    FiberPer100g = (double)item.Fiber,
                    IsVerified = true
                };
                _context.Foods.Add(food);
                added++;
            }
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Foods: {Added} added, {Updated} updated", added, updated);
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

    private string GetMuscleNameById(int id)
    {
        // Map from seed file IDs to muscle names (matches muscles.json)
        return id switch
        {
            1 => "Chest",
            2 => "Back",
            3 => "Shoulders",
            4 => "Biceps",
            5 => "Triceps",
            6 => "Forearms",
            7 => "Quadriceps",
            8 => "Hamstrings",
            9 => "Glutes",
            10 => "Calves",
            11 => "Abs",
            12 => "Obliques",
            13 => "Lower Back",
            14 => "Traps",
            15 => "Lats",
            _ => "Chest"
        };
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
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public string? BodyPart { get; set; }
}

public class ExerciseSeedDto
{
    public Guid? TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public int TargetMuscleId { get; set; }
    public string? Equipment { get; set; }
    public bool IsHighImpact { get; set; }
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
