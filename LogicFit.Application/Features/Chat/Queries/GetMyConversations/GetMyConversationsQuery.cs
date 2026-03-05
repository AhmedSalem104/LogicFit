using LogicFit.Application.Features.Chat.DTOs;
using MediatR;

namespace LogicFit.Application.Features.Chat.Queries.GetMyConversations;

public class GetMyConversationsQuery : IRequest<List<ConversationDto>>
{
}
