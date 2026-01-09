using LogicFit.Domain.Common;
using LogicFit.Domain.Common.Interfaces;
using LogicFit.Domain.Enums;

namespace LogicFit.Domain.Entities;

public class WalletTransaction : AuditableEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public string? Description { get; set; }
    public string? ReferenceType { get; set; }  // e.g., "Subscription", "Refund", "Manual"
    public Guid? ReferenceId { get; set; }      // e.g., SubscriptionId

    // Navigation Properties
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}
