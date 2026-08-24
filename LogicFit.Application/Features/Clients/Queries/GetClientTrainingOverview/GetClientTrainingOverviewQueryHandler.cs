using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.AthleteCheckins.Queries.GetAthleteCheckins;
using LogicFit.Application.Features.BodyMeasurements.Queries.GetBodyMeasurements;
using LogicFit.Application.Features.Clients.DTOs;
using LogicFit.Application.Features.Clients.Queries.GetClientById;
using LogicFit.Application.Features.DietPlans.Queries.GetDietPlans;
using LogicFit.Application.Features.MealLogs.Queries.GetMealLogs;
using LogicFit.Application.Features.Subscriptions.Queries.GetClientSubscriptions;
using LogicFit.Application.Features.WorkoutPrograms.Queries.GetWorkoutPrograms;
using LogicFit.Application.Features.WorkoutSessions.Queries.GetWorkoutSessions;
using MediatR;

namespace LogicFit.Application.Features.Clients.Queries.GetClientTrainingOverview;

public sealed class GetClientTrainingOverviewQueryHandler : IRequestHandler<GetClientTrainingOverviewQuery, ClientTrainingOverviewDto?>
{
    private readonly IMediator _mediator;

    public GetClientTrainingOverviewQueryHandler(IMediator mediator) => _mediator = mediator;

    public async Task<ClientTrainingOverviewDto?> Handle(GetClientTrainingOverviewQuery request, CancellationToken cancellationToken)
    {
        var client = await _mediator.Send(new GetClientByIdQuery { Id = request.ClientId }, cancellationToken);
        if (client == null) return null;

        var subscriptions = await _mediator.Send(new GetClientSubscriptionsQuery { ClientId = request.ClientId }, cancellationToken);
        var programs = await _mediator.Send(new GetWorkoutProgramsQuery { ClientId = request.ClientId }, cancellationToken);
        var diets = await _mediator.Send(new GetDietPlansQuery { ClientId = request.ClientId }, cancellationToken);
        var measurements = await _mediator.Send(new GetBodyMeasurementsQuery { ClientId = request.ClientId }, cancellationToken);
        var checkins = await _mediator.Send(new GetAthleteCheckinsQuery { ClientId = request.ClientId }, cancellationToken);
        var sessions = await _mediator.Send(new GetWorkoutSessionsQuery { ClientId = request.ClientId }, cancellationToken);
        var mealLogs = await _mediator.Send(new GetMealLogsQuery { ClientId = request.ClientId, AllDates = true }, cancellationToken);

        return new ClientTrainingOverviewDto
        {
            Client = client,
            Subscriptions = subscriptions,
            WorkoutPrograms = programs,
            DietPlans = diets,
            Measurements = measurements,
            Checkins = checkins,
            WorkoutSessions = sessions,
            MealLogs = mealLogs
        };
    }
}
