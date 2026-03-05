using LogicFit.Domain.Enums;

namespace LogicFit.Application.Features.Subscriptions.DTOs;

public class SubscriptionPlanDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int DurationMonths { get; set; }
    public string? Description { get; set; }
    public List<string> Features { get; set; } = new();
    public int MaxFreezeDays { get; set; }
    public int MaxFreezeCount { get; set; }
    public bool IsActive { get; set; }
    public int? SessionsPerWeek { get; set; }
    public bool InBodyIncluded { get; set; }
    public bool PrivateCoach { get; set; }
    public int ActiveSubscribersCount { get; set; }
}

public class ClientSubscriptionDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ClientId { get; set; }
    public string? ClientName { get; set; }
    public Guid PlanId { get; set; }
    public string? PlanName { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public SubscriptionStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public Guid? SalesCoachId { get; set; }
    public string? SalesCoachName { get; set; }

    // Payment
    public PaymentMethod? PaymentMethod { get; set; }
    public string? PaymentMethodName => PaymentMethod?.ToString();
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal RemainingAmount => TotalAmount - AmountPaid;
    public decimal Discount { get; set; }
    public bool IsPaid => AmountPaid >= TotalAmount && TotalAmount > 0;
    public string? Notes { get; set; }

    // Renewal
    public Guid? RenewedFromId { get; set; }

    // Computed
    public int RemainingDays => EndDate > DateTime.UtcNow ? (EndDate - DateTime.UtcNow).Days : 0;
    public int TotalFreezeDays { get; set; }

    public List<SubscriptionFreezeDto> Freezes { get; set; } = new();
}

public class ClientSubscriptionDetailDto : ClientSubscriptionDto
{
    public SubscriptionPlanDto? PlanDetails { get; set; }
    public string? ClientPhone { get; set; }
    public string? ClientEmail { get; set; }
    public List<RenewalHistoryDto> RenewalHistory { get; set; } = new();
}

public class RenewalHistoryDto
{
    public Guid Id { get; set; }
    public string? PlanName { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public SubscriptionStatus Status { get; set; }
    public decimal AmountPaid { get; set; }
}

public class SubscriptionFreezeDto
{
    public Guid Id { get; set; }
    public Guid SubscriptionId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Reason { get; set; }
    public bool IsActive { get; set; }
    public int DurationDays => (EndDate - StartDate).Days;
}
