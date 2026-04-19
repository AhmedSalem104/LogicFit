using MediatR;

namespace LogicFit.Application.Features.GroupClasses.Commands.DeleteGroupClass;

public class DeleteGroupClassCommand : IRequest
{
    public Guid Id { get; set; }
}
