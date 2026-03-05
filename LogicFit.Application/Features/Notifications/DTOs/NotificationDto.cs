using LogicFit.Domain.Enums;

namespace LogicFit.Application.Features.Notifications.DTOs;

public class NotificationDto
{
    public Guid Id { get; set; }
    public Guid SenderId { get; set; }
    public string? SenderName { get; set; }
    public Guid RecipientId { get; set; }
    public string? RecipientName { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
