using LogicFit.Application.Features.Maintenance.Commands.CreateMaintenance;
using LogicFit.Application.Features.Maintenance.Commands.ResolveMaintenance;
using LogicFit.Application.Features.Maintenance.DTOs;
using LogicFit.Application.Features.Maintenance.Queries.GetMaintenanceRecords;
using LogicFit.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogicFit.API.Features.Maintenance;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MaintenanceController : ControllerBase
{
    private readonly IMediator _mediator;

    public MaintenanceController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<MaintenanceRecordDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<MaintenanceRecordDto>>> GetRecords(
        [FromQuery] Guid? equipmentId,
        [FromQuery] MaintenanceStatus? status,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate)
    {
        var result = await _mediator.Send(new GetMaintenanceRecordsQuery
        {
            EquipmentId = equipmentId,
            Status = status,
            FromDate = fromDate,
            ToDate = toDate
        });
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> CreateMaintenance(CreateMaintenanceCommand command)
    {
        var id = await _mediator.Send(command);
        return Ok(id);
    }

    [HttpPost("{id}/resolve")]
    public async Task<ActionResult> Resolve(Guid id, ResolveMaintenanceCommand command)
    {
        command.Id = id;
        await _mediator.Send(command);
        return NoContent();
    }
}
