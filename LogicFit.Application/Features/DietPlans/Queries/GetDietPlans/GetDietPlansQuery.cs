using LogicFit.Application.Features.DietPlans.DTOs;
using LogicFit.Domain.Enums;
using MediatR;

namespace LogicFit.Application.Features.DietPlans.Queries.GetDietPlans;

public class GetDietPlansQuery : IRequest<List<DietPlanDto>>
{
    public Guid? CoachId { get; set; }
    public Guid? ClientId { get; set; }
    public PlanStatus? Status { get; set; }
}
