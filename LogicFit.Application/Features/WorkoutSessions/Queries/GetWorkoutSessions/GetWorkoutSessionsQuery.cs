using LogicFit.Application.Features.WorkoutSessions.DTOs;
using MediatR;

namespace LogicFit.Application.Features.WorkoutSessions.Queries.GetWorkoutSessions;

public class GetWorkoutSessionsQuery : IRequest<List<WorkoutSessionDto>>
{
    public Guid? ClientId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
