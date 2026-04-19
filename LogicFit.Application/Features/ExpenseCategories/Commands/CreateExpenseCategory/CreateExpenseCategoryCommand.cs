using MediatR;

namespace LogicFit.Application.Features.ExpenseCategories.Commands.CreateExpenseCategory;

public class CreateExpenseCategoryCommand : IRequest<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ParentCategoryId { get; set; }
    public bool IsActive { get; set; } = true;
}
