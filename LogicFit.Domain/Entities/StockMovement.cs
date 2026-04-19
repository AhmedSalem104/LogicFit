using LogicFit.Domain.Common;
using LogicFit.Domain.Enums;

namespace LogicFit.Domain.Entities;

public class StockMovement : TenantAuditableEntity
{
    public Guid ProductId { get; set; }
    public Guid BranchId { get; set; }
    public StockMovementType Type { get; set; }
    public decimal Quantity { get; set; } // positive; Type determines direction
    public decimal QuantityAfter { get; set; }
    public string? Reason { get; set; }
    public string? ReferenceType { get; set; } // Sale, Purchase, Transfer, Manual
    public Guid? ReferenceId { get; set; }
    public DateTime MovedAt { get; set; }
    public Guid? MovedById { get; set; }
    public Guid? TargetBranchId { get; set; } // for transfers

    public virtual Product Product { get; set; } = null!;
    public virtual Branch Branch { get; set; } = null!;
    public virtual User? MovedBy { get; set; }
    public virtual Branch? TargetBranch { get; set; }
}
