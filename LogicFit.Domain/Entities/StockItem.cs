using LogicFit.Domain.Common;

namespace LogicFit.Domain.Entities;

public class StockItem : TenantAuditableEntity
{
    public Guid ProductId { get; set; }
    public Guid BranchId { get; set; }
    public decimal Quantity { get; set; }
    public DateTime? LastMovementAt { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public virtual Product Product { get; set; } = null!;
    public virtual Branch Branch { get; set; } = null!;
}
