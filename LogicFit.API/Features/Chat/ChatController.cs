using LogicFit.Application.Features.Chat.Commands.MarkMessagesAsRead;
using LogicFit.Application.Features.Chat.Commands.SendMessage;
using LogicFit.Application.Features.Chat.DTOs;
using LogicFit.Application.Features.Chat.Queries.GetConversationMessages;
using LogicFit.Application.Features.Chat.Queries.GetMyConversations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogicFit.API.Features.Chat;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly IMediator _mediator;

    public ChatController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("conversations")]
    public async Task<ActionResult<List<ConversationDto>>> GetMyConversations()
    {
        var result = await _mediator.Send(new GetMyConversationsQuery());
        return Ok(result);
    }

    [HttpGet("conversations/{conversationId}/messages")]
    public async Task<ActionResult<List<ChatMessageDto>>> GetConversationMessages(Guid conversationId)
    {
        var result = await _mediator.Send(new GetConversationMessagesQuery { ConversationId = conversationId });
        return Ok(result);
    }

    [HttpPost("messages")]
    public async Task<ActionResult<Guid>> SendMessage([FromBody] SendMessageCommand command)
    {
        var id = await _mediator.Send(command);
        return Ok(id);
    }

    [HttpPut("conversations/{conversationId}/read")]
    public async Task<ActionResult> MarkMessagesAsRead(Guid conversationId)
    {
        await _mediator.Send(new MarkMessagesAsReadCommand { ConversationId = conversationId });
        return NoContent();
    }
}
