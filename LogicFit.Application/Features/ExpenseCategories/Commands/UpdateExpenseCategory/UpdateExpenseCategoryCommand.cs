using MediatR;

namespace LogicFit.Application.Features.ExpenseCategories.Commands.UpdateExpenseCategory;

public class UpdateExpenseCategoryCommand : IRequest
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ParentCategoryId { get; set; }
    public bool IsActive { get; set; } = true;
}
