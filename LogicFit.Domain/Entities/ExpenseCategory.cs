using LogicFit.Domain.Common;

namespace LogicFit.Domain.Entities;

public class ExpenseCategory : TenantAuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ParentCategoryId { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual ExpenseCategory? ParentCategory { get; set; }
    public virtual ICollection<ExpenseCategory> Children { get; set; } = new List<ExpenseCategory>();
    public virtual ICollection<Expense> Expenses { get; set; } = new List<Expense>();
}
