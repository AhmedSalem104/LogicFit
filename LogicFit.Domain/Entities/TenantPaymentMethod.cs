using LogicFit.Domain.Common;
using LogicFit.Domain.Common.Interfaces;

namespace LogicFit.Domain.Entities;

/// <summary>A manual payment channel configured by the platform (bank, InstaPay, wallet, cash…).</summary>
public class TenantPaymentMethod : AuditableEntity, ISoftDeletable
{
    public string Name { get; set; } = string.Empty;
    public string? Type { get; set; }
    public string? AccountName { get; set; }
    public string? AccountNumber { get; set; }
    public string? IBAN { get; set; }
    public string? WalletNumber { get; set; }
    public string? Instructions { get; set; }
    public string? QRImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }

    // Soft Delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
