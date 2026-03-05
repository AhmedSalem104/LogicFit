using LogicFit.Application.Features.ClientDashboard.DTOs;
using LogicFit.Application.Features.ClientDashboard.Queries.GetMyAppointments;
using LogicFit.Application.Features.ClientDashboard.Queries.GetMyCoach;
using LogicFit.Application.Features.ClientDashboard.Queries.GetMyDashboard;
using LogicFit.Application.Features.ClientDashboard.Queries.GetMyDietPlans;
using LogicFit.Application.Features.ClientDashboard.Queries.GetMyMeasurements;
using LogicFit.Application.Features.ClientDashboard.Queries.GetMyPrograms;
using LogicFit.Application.Features.ClientDashboard.Queries.GetMySubscriptions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogicFit.API.Features.ClientDashboard;

[ApiController]
[Route("api/client")]
[Authorize]
public class ClientDashboardController : ControllerBase
{
    private readonly IMediator _mediator;

    public ClientDashboardController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<ClientDashboardDto>> GetMyDashboard()
    {
        var result = await _mediator.Send(new GetMyDashboardQuery());
        return Ok(result);
    }

    [HttpGet("my-programs")]
    public async Task<ActionResult<List<MyWorkoutProgramDto>>> GetMyPrograms()
    {
        var result = await _mediator.Send(new GetMyProgramsQuery());
        return Ok(result);
    }

    [HttpGet("my-diet-plans")]
    public async Task<ActionResult<List<MyDietPlanDto>>> GetMyDietPlans()
    {
        var result = await _mediator.Send(new GetMyDietPlansQuery());
        return Ok(result);
    }

    [HttpGet("my-subscriptions")]
    public async Task<ActionResult<List<MySubscriptionSummaryDto>>> GetMySubscriptions()
    {
        var result = await _mediator.Send(new GetMySubscriptionsQuery());
        return Ok(result);
    }

    [HttpGet("my-measurements")]
    public async Task<ActionResult<List<MyBodyMeasurementDto>>> GetMyMeasurements()
    {
        var result = await _mediator.Send(new GetMyMeasurementsQuery());
        return Ok(result);
    }

    [HttpGet("my-coach")]
    public async Task<ActionResult<MyCoachDto>> GetMyCoach()
    {
        var result = await _mediator.Send(new GetMyCoachQuery());
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpGet("my-appointments")]
    public async Task<ActionResult<List<MyAppointmentDto>>> GetMyAppointments()
    {
        var result = await _mediator.Send(new GetMyAppointmentsQuery());
        return Ok(result);
    }
}
