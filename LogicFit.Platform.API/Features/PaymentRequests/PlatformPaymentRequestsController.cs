using LogicFit.Application.Features.Platform.PaymentRequests.Commands.ApprovePaymentRequest;
using LogicFit.Application.Features.Platform.PaymentRequests.Commands.RejectPaymentRequest;
using LogicFit.Application.Features.Platform.PaymentRequests.DTOs;
using LogicFit.Application.Features.Platform.PaymentRequests.Queries.GetPaymentRequests;
using LogicFit.Domain.Authorization;
using LogicFit.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogicFit.Platform.API.Features.PaymentRequests;

[ApiController]
[Route("api/platform/payment-requests")]
[Authorize(Policy = Permissions.ManagePaymentRequests)]
public class PlatformPaymentRequestsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PlatformPaymentRequestsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<PaymentRequestDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<PaymentRequestDto>>> Get([FromQuery] PaymentRequestStatus? status)
    {
        var result = await _mediator.Send(new GetPaymentRequestsQuery { Status = status });
        return Ok(result);
    }

    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType(typeof(PaymentRequestDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaymentRequestDto>> Approve(Guid id)
    {
        var result = await _mediator.Send(new ApprovePaymentRequestCommand(id));
        return Ok(result);
    }

    [HttpPost("{id:guid}/reject")]
    [ProducesResponseType(typeof(PaymentRequestDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaymentRequestDto>> Reject(Guid id, [FromBody] RejectPaymentRequestCommand command)
    {
        command.PaymentRequestId = id;
        var result = await _mediator.Send(command);
        return Ok(result);
    }
}
