using LogicFit.Domain.Enums;
using MediatR;

namespace LogicFit.Application.Features.Clients.Commands.OnboardClient;

public sealed class OnboardClientCommand : IRequest<OnboardClientResult>
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Password { get; set; }
    public string? FullName { get; set; }
    public int? Gender { get; set; }
    public DateTime? BirthDate { get; set; }
    public Guid? CoachId { get; set; }
    public MembershipDetails? Membership { get; set; }
}

public sealed class MembershipDetails
{
    public Guid PlanId { get; set; }
    public DateTime StartDate { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    public decimal? AmountPaid { get; set; }
    public decimal? Discount { get; set; }
    public string? Notes { get; set; }
    public bool PayFromWallet { get; set; }
    public bool IssueCard { get; set; } = true;
}

public sealed class OnboardClientResult
{
    public Guid ClientId { get; init; }
    public Guid? SubscriptionId { get; init; }
    public Guid? MembershipCardId { get; init; }
}
