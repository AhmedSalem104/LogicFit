using LogicFit.Application.Features.ClientDashboard.DTOs;
using MediatR;

namespace LogicFit.Application.Features.ClientDashboard.Queries.GetMyPrograms;

public class GetMyProgramsQuery : IRequest<List<MyWorkoutProgramDto>>
{
}
