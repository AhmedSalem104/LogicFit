using LogicFit.Application.Features.BodyMeasurements.Commands.CreateBodyMeasurement;
using LogicFit.Application.Features.BodyMeasurements.Commands.DeleteBodyMeasurement;
using LogicFit.Application.Features.BodyMeasurements.DTOs;
using LogicFit.Application.Features.BodyMeasurements.Queries.GetBodyMeasurements;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogicFit.API.Features.BodyMeasurements;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BodyMeasurementsController : ControllerBase
{
    private readonly IMediator _mediator;

    public BodyMeasurementsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<List<BodyMeasurementDto>>> GetBodyMeasurements(
        [FromQuery] Guid? clientId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate)
    {
        var result = await _mediator.Send(new GetBodyMeasurementsQuery
        {
            ClientId = clientId,
            FromDate = fromDate,
            ToDate = toDate
        });
        return Ok(result);
    }

    /// <summary>
    /// Create body measurement (JSON - without images)
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Guid>> CreateBodyMeasurement([FromBody] CreateBodyMeasurementCommand command)
    {
        var id = await _mediator.Send(command);
        return Ok(id);
    }

    /// <summary>
    /// Create body measurement with images (multipart/form-data)
    /// </summary>
    [HttpPost("with-images")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<Guid>> CreateBodyMeasurementWithImages([FromForm] CreateBodyMeasurementCommand command)
    {
        var id = await _mediator.Send(command);
        return Ok(id);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteBodyMeasurement(Guid id)
    {
        await _mediator.Send(new DeleteBodyMeasurementCommand { Id = id });
        return NoContent();
    }
}
