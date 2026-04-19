using MediatR;

namespace LogicFit.Application.Features.Expenses.Commands.DeleteExpense;

public class DeleteExpenseCommand : IRequest
{
    public Guid Id { get; set; }
}
