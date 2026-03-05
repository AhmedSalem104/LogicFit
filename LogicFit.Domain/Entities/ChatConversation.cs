using LogicFit.Domain.Common;

namespace LogicFit.Domain.Entities;

public class ChatConversation : TenantAuditableEntity
{
    public Guid CoachId { get; set; }
    public Guid ClientId { get; set; }
    public DateTime? LastMessageAt { get; set; }

    // Navigation Properties
    public virtual User Coach { get; set; } = null!;
    public virtual User Client { get; set; } = null!;
    public virtual ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}
