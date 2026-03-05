using LogicFit.Domain.Common;
using LogicFit.Domain.Enums;

namespace LogicFit.Domain.Entities;

public class Notification : TenantAuditableEntity
{
    public Guid SenderId { get; set; }
    public Guid RecipientId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }

    // Navigation Properties
    public virtual User Sender { get; set; } = null!;
    public virtual User Recipient { get; set; } = null!;
}
