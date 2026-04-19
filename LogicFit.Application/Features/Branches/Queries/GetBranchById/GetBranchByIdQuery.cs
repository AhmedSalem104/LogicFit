using LogicFit.Application.Features.Branches.DTOs;
using MediatR;

namespace LogicFit.Application.Features.Branches.Queries.GetBranchById;

public class GetBranchByIdQuery : IRequest<BranchDto?>
{
    public Guid Id { get; set; }
}
