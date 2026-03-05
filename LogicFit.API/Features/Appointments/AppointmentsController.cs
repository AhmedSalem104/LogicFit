using LogicFit.Application.Features.Appointments.Commands.CreateAppointment;
using LogicFit.Application.Features.Appointments.Commands.DeleteAppointment;
using LogicFit.Application.Features.Appointments.Commands.UpdateAppointmentStatus;
using LogicFit.Application.Features.Appointments.DTOs;
using LogicFit.Application.Features.Appointments.Queries.GetAppointmentById;
using LogicFit.Application.Features.Appointments.Queries.GetAppointments;
using LogicFit.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogicFit.API.Features.Appointments;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AppointmentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AppointmentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<List<AppointmentDto>>> GetAppointments(
        [FromQuery] Guid? coachId,
        [FromQuery] Guid? clientId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] AppointmentStatus? status)
    {
        var result = await _mediator.Send(new GetAppointmentsQuery
        {
            CoachId = coachId,
            ClientId = clientId,
            FromDate = fromDate,
            ToDate = toDate,
            Status = status
        });
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AppointmentDto>> GetAppointmentById(Guid id)
    {
        var result = await _mediator.Send(new GetAppointmentByIdQuery { Id = id });
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> CreateAppointment([FromBody] CreateAppointmentCommand command)
    {
        var id = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetAppointmentById), new { id }, id);
    }

    [HttpPut("{id}/status")]
    public async Task<ActionResult> UpdateAppointmentStatus(Guid id, [FromBody] UpdateAppointmentStatusCommand command)
    {
        command.Id = id;
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteAppointment(Guid id)
    {
        await _mediator.Send(new DeleteAppointmentCommand { Id = id });
        return NoContent();
    }
}
