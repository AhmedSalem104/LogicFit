using LogicFit.Domain.Common.Interfaces;

namespace LogicFit.Domain.Common;

public abstract class AuditableEntity : BaseEntity, IAuditableEntity
{
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
