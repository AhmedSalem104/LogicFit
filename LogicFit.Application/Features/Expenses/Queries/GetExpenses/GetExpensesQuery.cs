using LogicFit.Application.Features.Expenses.DTOs;
using MediatR;

namespace LogicFit.Application.Features.Expenses.Queries.GetExpenses;

public class GetExpensesQuery : IRequest<List<ExpenseDto>>
{
    public Guid? BranchId { get; set; }
    public Guid? CategoryId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? SearchTerm { get; set; }
}
