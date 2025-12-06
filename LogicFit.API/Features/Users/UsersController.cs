using LogicFit.Application.Features.Users.Commands.UpdateUser;
using LogicFit.Application.Features.Users.Commands.UpdateUserProfile;
using LogicFit.Application.Features.Users.DTOs;
using LogicFit.Application.Features.Users.Queries.GetUserById;
using LogicFit.Application.Features.Users.Queries.GetUsers;
using LogicFit.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogicFit.API.Features.Users;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<List<UserDto>>> GetUsers(
        [FromQuery] UserRole? role,
        [FromQuery] bool? isActive,
        [FromQuery] string? searchTerm)
    {
        var result = await _mediator.Send(new GetUsersQuery
        {
            Role = role,
            IsActive = isActive,
            SearchTerm = searchTerm
        });
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetUser(Guid id)
    {
        var result = await _mediator.Send(new GetUserByIdQuery { Id = id });
        if (result == null)
            return NotFound();
        return Ok(result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateUser(Guid id, UpdateUserCommand command)
    {
        command.Id = id;
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpPut("{id}/profile")]
    public async Task<ActionResult> UpdateUserProfile(Guid id, UpdateUserProfileCommand command)
    {
        command.UserId = id;
        await _mediator.Send(command);
        return NoContent();
    }
}
