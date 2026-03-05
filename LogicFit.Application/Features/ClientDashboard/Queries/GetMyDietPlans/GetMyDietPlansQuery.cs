using LogicFit.Application.Features.ClientDashboard.DTOs;
using MediatR;

namespace LogicFit.Application.Features.ClientDashboard.Queries.GetMyDietPlans;

public class GetMyDietPlansQuery : IRequest<List<MyDietPlanDto>>
{
}
