using MediatR;

namespace LogicFit.Application.Features.Chat.Commands.SendMessage;

public class SendMessageCommand : IRequest<Guid>
{
    public Guid? ConversationId { get; set; }
    public Guid? RecipientId { get; set; }
    public string Content { get; set; } = string.Empty;
}
