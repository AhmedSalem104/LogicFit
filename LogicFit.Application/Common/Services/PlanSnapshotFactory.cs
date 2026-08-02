using System.Text.Json;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;

namespace LogicFit.Application.Common.Services;

/// <summary>Creates an immutable, reviewable representation of a plan at selection time.</summary>
public static class PlanSnapshotFactory
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static string Create(Plan plan, BillingCycle billingCycle, DateTime capturedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(plan);
        return JsonSerializer.Serialize(new
        {
            planId = plan.Id,
            planName = plan.Name,
            billingCycle,
            basePrice = plan.Price,
            discount = 0m,
            finalAmount = plan.Price,
            currency = plan.Currency,
            limits = new
            {
                plan.MaxMembers,
                plan.MaxCoaches,
                plan.MaxBranches,
                plan.MaxEmployees,
                plan.MaxStorageMB
            },
            features = plan.PlanFeatures
                .Where(x => x.Feature is not null)
                .Select(x => new { code = x.Feature.Code, x.LimitValue })
                .ToArray(),
            snapshotAt = capturedAtUtc
        }, JsonOptions);
    }
}
