using LogicFit.Domain.Common.Interfaces;

namespace LogicFit.Domain.Common;

public abstract class TenantAuditableEntity : AuditableEntity, ITenantEntity, ISoftDeletable
{
    public Guid TenantId { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
