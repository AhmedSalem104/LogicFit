using LogicFit.Domain.Enums;

namespace LogicFit.Application.Features.Subscriptions.DTOs;

public class SubscriptionPlanDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int DurationMonths { get; set; }
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
    public Guid? SalesCoachId { get; set; }
    public string? SalesCoachName { get; set; }
    public List<SubscriptionFreezeDto> Freezes { get; set; } = new();
}

public class SubscriptionFreezeDto
{
    public Guid Id { get; set; }
    public Guid SubscriptionId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Reason { get; set; }
}

public class CreateSubscriptionPlanDto
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int DurationMonths { get; set; }
}

public class CreateClientSubscriptionDto
{
    public Guid ClientId { get; set; }
    public Guid PlanId { get; set; }
    public DateTime StartDate { get; set; }
}

public class CreateSubscriptionFreezeDto
{
    public Guid SubscriptionId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Reason { get; set; }
}
