using MediatR;

namespace LogicFit.Application.Features.Chat.Commands.MarkMessagesAsRead;

public class MarkMessagesAsReadCommand : IRequest<bool>
{
    public Guid ConversationId { get; set; }
}
