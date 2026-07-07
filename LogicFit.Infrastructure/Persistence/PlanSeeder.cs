using LogicFit.Domain.Authorization;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LogicFit.Infrastructure.Persistence;

/// <summary>Seeds the SaaS feature catalog and default plans (Starter/Professional/Enterprise). Idempotent.</summary>
public class PlanSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PlanSeeder> _logger;

    public PlanSeeder(ApplicationDbContext context, ILogger<PlanSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    private static readonly Dictionary<string, string> FeatureNames = new()
    {
        [FeatureCodes.POS] = "Point of Sale",
        [FeatureCodes.Inventory] = "Inventory Management",
        [FeatureCodes.AdvancedReports] = "Advanced Reports",
        [FeatureCodes.MultiBranch] = "Multi-Branch",
        [FeatureCodes.WhiteLabel] = "White Label",
        [FeatureCodes.EmployeeManagement] = "Employee Management",
        [FeatureCodes.FinanceModule] = "Finance Module",
        [FeatureCodes.ClientMobileApp] = "Client Mobile App",
        [FeatureCodes.CustomDomain] = "Custom Domain"
    };

    private record PlanSeed(
        string Name, decimal Price, int DurationInDays,
        int? MaxMembers, int? MaxCoaches, int? MaxBranches, int? MaxEmployees, int? MaxStorageMB,
        int DisplayOrder, string[] Features);

    private static readonly PlanSeed[] Plans =
    {
        new("Starter", 0m, 30, 100, 2, 1, 3, 1024, 1,
            new[] { FeatureCodes.FinanceModule }),
        new("Professional", 499m, 30, 500, 10, 3, 20, 10240, 2,
            new[] { FeatureCodes.POS, FeatureCodes.Inventory, FeatureCodes.AdvancedReports,
                    FeatureCodes.MultiBranch, FeatureCodes.EmployeeManagement, FeatureCodes.FinanceModule,
                    FeatureCodes.ClientMobileApp }),
        new("Enterprise", 1499m, 30, null, null, null, null, null, 3,
            FeatureCodes.All.ToArray())
    };

    public async Task SeedAsync()
    {
        await SeedFeaturesAsync();
        await SeedPlansAsync();
        _logger.LogInformation("Plan/Feature seeding completed");
    }

    private async Task SeedFeaturesAsync()
    {
        var existing = await _context.Features.Select(f => f.Code).ToListAsync();
        var missing = FeatureCodes.All.Where(c => !existing.Contains(c)).ToList();

        foreach (var code in missing)
        {
            _context.Features.Add(new Feature
            {
                Code = code,
                Name = FeatureNames.TryGetValue(code, out var name) ? name : code,
                IsActive = true
            });
        }

        if (missing.Count > 0)
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Seeded {Count} features", missing.Count);
        }
    }

    private async Task SeedPlansAsync()
    {
        var featuresByCode = await _context.Features.ToDictionaryAsync(f => f.Code, f => f.Id);

        foreach (var seed in Plans)
        {
            var plan = await _context.Plans
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.Name == seed.Name);

            if (plan == null)
            {
                plan = new Plan
                {
                    Name = seed.Name,
                    Price = seed.Price,
                    Currency = "EGP",
                    BillingCycle = BillingCycle.Monthly,
                    DurationInDays = seed.DurationInDays,
                    MaxMembers = seed.MaxMembers,
                    MaxCoaches = seed.MaxCoaches,
                    MaxBranches = seed.MaxBranches,
                    MaxEmployees = seed.MaxEmployees,
                    MaxStorageMB = seed.MaxStorageMB,
                    IsActive = true,
                    DisplayOrder = seed.DisplayOrder
                };
                _context.Plans.Add(plan);
                await _context.SaveChangesAsync();
            }

            var existingFeatureIds = await _context.PlanFeatures
                .Where(pf => pf.PlanId == plan.Id)
                .Select(pf => pf.FeatureId)
                .ToListAsync();

            foreach (var code in seed.Features)
            {
                if (!featuresByCode.TryGetValue(code, out var featureId)) continue;
                if (existingFeatureIds.Contains(featureId)) continue;

                _context.PlanFeatures.Add(new PlanFeature { PlanId = plan.Id, FeatureId = featureId });
            }
        }

        await _context.SaveChangesAsync();
    }
}
