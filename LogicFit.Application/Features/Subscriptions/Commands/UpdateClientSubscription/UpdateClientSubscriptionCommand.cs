using MediatR;

namespace LogicFit.Application.Features.Subscriptions.Commands.UpdateClientSubscription;

public class UpdateClientSubscriptionCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Notes { get; set; }
    public decimal? AmountPaid { get; set; }
    public decimal? Discount { get; set; }
}
