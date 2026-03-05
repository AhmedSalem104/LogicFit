namespace LogicFit.Application.Features.Chat.DTOs;

public class ConversationDto
{
    public Guid Id { get; set; }
    public Guid CoachId { get; set; }
    public string? CoachName { get; set; }
    public Guid ClientId { get; set; }
    public string? ClientName { get; set; }
    public DateTime? LastMessageAt { get; set; }
    public string? LastMessagePreview { get; set; }
    public int UnreadCount { get; set; }
}

public class ChatMessageDto
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public Guid SenderId { get; set; }
    public string? SenderName { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
