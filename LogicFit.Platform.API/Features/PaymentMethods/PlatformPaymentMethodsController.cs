using LogicFit.Application.Features.Platform.PaymentMethods.Commands.DeletePaymentMethod;
using LogicFit.Application.Features.Platform.PaymentMethods.Commands.SavePaymentMethod;
using LogicFit.Application.Features.Platform.PaymentMethods.DTOs;
using LogicFit.Application.Features.Platform.PaymentMethods.Queries.GetPaymentMethods;
using LogicFit.Domain.Authorization;
using LogicFit.Platform.API.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogicFit.Platform.API.Features.PaymentMethods;

[ApiController]
[Route("api/platform/payment-methods")]
[Authorize(Policy = Permissions.ManagePaymentRequests)]
public class PlatformPaymentMethodsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PlatformPaymentMethodsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(
        [FromQuery] bool activeOnly = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PlatformPaging.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetPaymentMethodsQuery { ActiveOnly = activeOnly }, cancellationToken);
        return Ok(PlatformPaging.Create(result, page, pageSize));
    }

    [HttpPost]
    [ProducesResponseType(typeof(PaymentMethodDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<PaymentMethodDto>> Create([FromBody] SavePaymentMethodCommand command)
    {
        command.Id = null;
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(PaymentMethodDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaymentMethodDto>> Update(Guid id, [FromBody] SavePaymentMethodCommand command)
    {
        command.Id = id;
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _mediator.Send(new DeletePaymentMethodCommand(id));
        return NoContent();
    }
}
