using LogicFit.Application.Features.ExpenseCategories.DTOs;
using MediatR;

namespace LogicFit.Application.Features.ExpenseCategories.Queries.GetExpenseCategories;

public class GetExpenseCategoriesQuery : IRequest<List<ExpenseCategoryDto>>
{
    public bool? IsActive { get; set; }
}
