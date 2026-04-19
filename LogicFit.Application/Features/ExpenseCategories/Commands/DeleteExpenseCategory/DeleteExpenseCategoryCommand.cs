using MediatR;

namespace LogicFit.Application.Features.ExpenseCategories.Commands.DeleteExpenseCategory;

public class DeleteExpenseCategoryCommand : IRequest
{
    public Guid Id { get; set; }
}
