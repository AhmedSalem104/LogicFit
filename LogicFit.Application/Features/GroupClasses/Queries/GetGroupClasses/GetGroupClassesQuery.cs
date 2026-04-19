using LogicFit.Application.Features.GroupClasses.DTOs;
using MediatR;

namespace LogicFit.Application.Features.GroupClasses.Queries.GetGroupClasses;

public class GetGroupClassesQuery : IRequest<List<GroupClassDto>>
{
    public Guid? BranchId { get; set; }
    public bool? IsActive { get; set; }
    public string? Category { get; set; }
}
