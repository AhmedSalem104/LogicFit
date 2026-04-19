using LogicFit.Application.Features.Branches.DTOs;
using MediatR;

namespace LogicFit.Application.Features.Branches.Queries.GetBranches;

public class GetBranchesQuery : IRequest<List<BranchDto>>
{
    public bool? IsActive { get; set; }
    public string? SearchTerm { get; set; }
}
