using LogicFit.Application.Features.Users.DTOs;
using MediatR;

namespace LogicFit.Application.Features.Users.Queries.GetUserById;

public class GetUserByIdQuery : IRequest<UserDto?>
{
    public Guid Id { get; set; }
}
