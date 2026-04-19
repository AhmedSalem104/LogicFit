using LogicFit.Domain.Common;
using LogicFit.Domain.Enums;

namespace LogicFit.Domain.Entities;

public class PayrollRun : TenantAuditableEntity
{
    public Guid? BranchId { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public PayrollStatus Status { get; set; } = PayrollStatus.Draft;
    public decimal TotalAmount { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? Notes { get; set; }

    public virtual Branch? Branch { get; set; }
    public virtual ICollection<PayrollItem> Items { get; set; } = new List<PayrollItem>();
}
