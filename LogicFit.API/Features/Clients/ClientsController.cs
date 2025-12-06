using LogicFit.Application.Features.Clients.Commands.CreateClient;
using LogicFit.Application.Features.Clients.Commands.DeleteClient;
using LogicFit.Application.Features.Clients.Commands.UpdateClient;
using LogicFit.Application.Features.Clients.DTOs;
using LogicFit.Application.Features.Clients.Queries.GetClientById;
using LogicFit.Application.Features.Clients.Queries.GetClients;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogicFit.API.Features.Clients;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ClientsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ClientsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<List<ClientDto>>> GetClients(
        [FromQuery] string? searchTerm,
        [FromQuery] bool? isActive)
    {
        var result = await _mediator.Send(new GetClientsQuery
        {
            SearchTerm = searchTerm,
            IsActive = isActive
        });
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ClientDto>> GetClient(Guid id)
    {
        var result = await _mediator.Send(new GetClientByIdQuery { Id = id });
        if (result == null)
            return NotFound();
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> CreateClient(CreateClientCommand command)
    {
        var id = await _mediator.Send(command);
        return Ok(id);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateClient(Guid id, UpdateClientCommand command)
    {
        command.Id = id;
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteClient(Guid id)
    {
        await _mediator.Send(new DeleteClientCommand { Id = id });
        return NoContent();
    }
}
