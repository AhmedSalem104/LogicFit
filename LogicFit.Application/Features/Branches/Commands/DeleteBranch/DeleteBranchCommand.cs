using MediatR;

namespace LogicFit.Application.Features.Branches.Commands.DeleteBranch;

public class DeleteBranchCommand : IRequest
{
    public Guid Id { get; set; }
}
