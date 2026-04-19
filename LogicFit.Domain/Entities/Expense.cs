using LogicFit.Domain.Common;
using LogicFit.Domain.Enums;

namespace LogicFit.Domain.Entities;

public class Expense : TenantAuditableEntity
{
    public Guid? BranchId { get; set; }
    public Guid CategoryId { get; set; }
    public decimal Amount { get; set; }
    public DateTime ExpenseDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? VendorName { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    public string? ReceiptImageUrl { get; set; }
    public string? ReferenceNumber { get; set; }
    public Guid? ApprovedById { get; set; }
    public DateTime? ApprovedAt { get; set; }

    public virtual Branch? Branch { get; set; }
    public virtual ExpenseCategory Category { get; set; } = null!;
    public virtual User? ApprovedBy { get; set; }
}
