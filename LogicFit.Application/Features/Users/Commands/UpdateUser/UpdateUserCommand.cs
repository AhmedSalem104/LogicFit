using MediatR;

namespace LogicFit.Application.Features.Users.Commands.UpdateUser;

public class UpdateUserCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; }
}
