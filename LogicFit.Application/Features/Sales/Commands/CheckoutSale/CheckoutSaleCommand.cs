using LogicFit.Domain.Enums;
using MediatR;

namespace LogicFit.Application.Features.Sales.Commands.CheckoutSale;

public class CheckoutSaleCommand : IRequest<Guid>
{
    public Guid BranchId { get; set; }
    public Guid? ClientId { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
    public Guid? CouponId { get; set; }
    public decimal ExtraDiscount { get; set; }
    public string? Notes { get; set; }
    public List<CheckoutItem> Items { get; set; } = new();
}

public class CheckoutItem
{
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; } = 1;
    public decimal? UnitPriceOverride { get; set; }
    public decimal DiscountAmount { get; set; }
}
