using LogicFit.Application.Features.Platform.Plans.DTOs;
using MediatR;

namespace LogicFit.Application.Features.Platform.Plans.Queries.GetPlans;

public class GetPlansQuery : IRequest<List<PlanDto>>
{
    /// <summary>When true, only active plans are returned.</summary>
    public bool ActiveOnly { get; set; }
}
