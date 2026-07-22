using FluentValidation;

namespace LogicFit.Application.Features.Sales.Commands.CheckoutSale;

public sealed class CheckoutSaleCommandValidator : AbstractValidator<CheckoutSaleCommand>
{
    public CheckoutSaleCommandValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.Items).NotEmpty().WithMessage("Sale must have at least one item.");
        RuleFor(x => x.Items).Must(items => items.Select(i => i.ProductId).Distinct().Count() == items.Count)
            .WithMessage("A product may appear only once in a sale.");
        RuleFor(x => x.ExtraDiscount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Notes).MaximumLength(1000).When(x => x.Notes != null);

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).NotEmpty();
            item.RuleFor(i => i.Quantity).GreaterThan(0);
            item.RuleFor(i => i.UnitPriceOverride).GreaterThanOrEqualTo(0).When(i => i.UnitPriceOverride.HasValue);
            item.RuleFor(i => i.DiscountAmount).GreaterThanOrEqualTo(0);
        });
    }
}
