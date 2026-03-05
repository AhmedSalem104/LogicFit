using MediatR;

namespace LogicFit.Application.Features.Subscriptions.Commands.CreateSubscriptionPlan;

public class CreateSubscriptionPlanCommand : IRequest<Guid>
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int DurationMonths { get; set; }
    public string? Description { get; set; }
    public List<string>? Features { get; set; }
    public int MaxFreezeDays { get; set; }
    public int MaxFreezeCount { get; set; }
    public bool IsActive { get; set; } = true;
    public int? SessionsPerWeek { get; set; }
    public bool InBodyIncluded { get; set; }
    public bool PrivateCoach { get; set; }
}
