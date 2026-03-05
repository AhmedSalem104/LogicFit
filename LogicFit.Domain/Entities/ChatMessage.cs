using LogicFit.Domain.Common;

namespace LogicFit.Domain.Entities;

public class ChatMessage : TenantAuditableEntity
{
    public Guid ConversationId { get; set; }
    public Guid SenderId { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }

    // Navigation Properties
    public virtual ChatConversation Conversation { get; set; } = null!;
    public virtual User Sender { get; set; } = null!;
}
