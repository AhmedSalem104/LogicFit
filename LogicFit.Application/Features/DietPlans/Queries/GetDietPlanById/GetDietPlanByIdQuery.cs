using LogicFit.Application.Features.DietPlans.DTOs;
using MediatR;

namespace LogicFit.Application.Features.DietPlans.Queries.GetDietPlanById;

public class GetDietPlanByIdQuery : IRequest<DietPlanDto?>
{
    public Guid Id { get; set; }
}
