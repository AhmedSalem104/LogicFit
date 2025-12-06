using LogicFit.Application.Features.WorkoutSessions.DTOs;
using MediatR;

namespace LogicFit.Application.Features.WorkoutSessions.Queries.GetWorkoutSessionById;

public class GetWorkoutSessionByIdQuery : IRequest<WorkoutSessionDto?>
{
    public Guid Id { get; set; }
}
