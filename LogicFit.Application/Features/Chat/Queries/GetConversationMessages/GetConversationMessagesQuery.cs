using LogicFit.Application.Features.Chat.DTOs;
using MediatR;

namespace LogicFit.Application.Features.Chat.Queries.GetConversationMessages;

public class GetConversationMessagesQuery : IRequest<List<ChatMessageDto>>
{
    public Guid ConversationId { get; set; }
}
