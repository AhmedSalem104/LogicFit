using LogicFit.Application.Features.Users.DTOs;
using LogicFit.Domain.Enums;
using MediatR;

namespace LogicFit.Application.Features.Users.Queries.GetUsers;

public class GetUsersQuery : IRequest<List<UserDto>>
{
    public UserRole? Role { get; set; }
    public bool? IsActive { get; set; }
    public string? SearchTerm { get; set; }
}
