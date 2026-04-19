using LogicFit.Application.Features.ClassEnrollments.Commands.BookClass;
using LogicFit.Application.Features.ClassEnrollments.Commands.CancelEnrollment;
using LogicFit.Application.Features.ClassEnrollments.Commands.MarkAttended;
using LogicFit.Application.Features.ClassEnrollments.Queries.GetScheduleEnrollments;
using LogicFit.Application.Features.ClassSchedules.Commands.CancelClassSchedule;
using LogicFit.Application.Features.ClassSchedules.Commands.CreateClassSchedule;
using LogicFit.Application.Features.ClassSchedules.Queries.GetClassSchedules;
using LogicFit.Application.Features.GroupClasses.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogicFit.API.Features.ClassSchedules;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ClassSchedulesController : ControllerBase
{
    private readonly IMediator _mediator;
    public ClassSchedulesController(IMediator mediator) { _mediator = mediator; }

    [HttpGet]
    public async Task<ActionResult<List<ClassScheduleDto>>> GetSchedules(
        [FromQuery] Guid? groupClassId,
        [FromQuery] Guid? coachId,
        [FromQuery] Guid? roomId,
        [FromQuery] Guid? branchId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] bool? includeCancelled)
        => Ok(await _mediator.Send(new GetClassSchedulesQuery
        {
            GroupClassId = groupClassId,
            CoachId = coachId,
            RoomId = roomId,
            BranchId = branchId,
            FromDate = fromDate,
            ToDate = toDate,
            IncludeCancelled = includeCancelled
        }));

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateClassScheduleCommand command)
        => Ok(await _mediator.Send(command));

    [HttpPost("{id}/cancel")]
    public async Task<ActionResult> Cancel(Guid id, [FromBody] CancelClassScheduleCommand command)
    {
        command.Id = id;
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpGet("{id}/enrollments")]
    public async Task<ActionResult<List<ClassEnrollmentDto>>> GetEnrollments(Guid id, [FromQuery] bool includeCancelled = false)
        => Ok(await _mediator.Send(new GetScheduleEnrollmentsQuery { ScheduleId = id, IncludeCancelled = includeCancelled }));

    [HttpPost("{id}/book")]
    public async Task<ActionResult<ClassEnrollmentDto>> Book(Guid id, [FromBody] BookClassCommand command)
    {
        command.ScheduleId = id;
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpPost("enrollments/{enrollmentId}/cancel")]
    public async Task<ActionResult> CancelEnrollment(Guid enrollmentId, [FromBody] CancelEnrollmentCommand command)
    {
        command.Id = enrollmentId;
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpPost("enrollments/{enrollmentId}/attended")]
    public async Task<ActionResult> MarkAttended(Guid enrollmentId)
    {
        await _mediator.Send(new MarkAttendedCommand { Id = enrollmentId });
        return NoContent();
    }
}
