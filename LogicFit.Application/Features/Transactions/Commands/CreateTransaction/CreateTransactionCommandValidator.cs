using FluentValidation;

namespace LogicFit.Application.Features.Transactions.Commands.CreateTransaction;

public class CreateTransactionCommandValidator : AbstractValidator<CreateTransactionCommand>
{
    public CreateTransactionCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("معرف المستخدم مطلوب");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("نوع المعاملة غير صالح");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("المبلغ يجب أن يكون أكبر من صفر");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("الوصف يجب ألا يتجاوز 500 حرف");

        RuleFor(x => x.ReferenceType)
            .MaximumLength(50).WithMessage("نوع المرجع يجب ألا يتجاوز 50 حرف");
    }
}
