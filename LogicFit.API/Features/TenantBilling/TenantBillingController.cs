using LogicFit.Application.Common.Authorization;
using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Platform.PaymentMethods.DTOs;
using LogicFit.Application.Features.Platform.PaymentMethods.Queries.GetPaymentMethods;
using LogicFit.Application.Features.Platform.PaymentRequests.DTOs;
using LogicFit.Application.Features.TenantBilling.Commands.SubmitPaymentRequest;
using LogicFit.Application.Features.TenantBilling.Queries.GetMyPaymentRequests;
using LogicFit.Domain.Authorization;
using LogicFit.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogicFit.API.Features.TenantBilling;

[ApiController]
[Route("api/tenant")]
[Authorize(Policy = Permissions.ManageTenantBilling)]
[AllowWhenPendingApproval] // reachable while the gym is PendingApproval so the owner can pay & onboard
public class TenantBillingController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IFileUploadService _fileUploadService;

    public TenantBillingController(IMediator mediator, IFileUploadService fileUploadService)
    {
        _mediator = mediator;
        _fileUploadService = fileUploadService;
    }

    /// <summary>Active manual payment channels the owner can pay through.</summary>
    [HttpGet("payment-methods")]
    [ProducesResponseType(typeof(List<PaymentMethodDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<PaymentMethodDto>>> GetPaymentMethods()
    {
        var result = await _mediator.Send(new GetPaymentMethodsQuery { ActiveOnly = true });
        return Ok(result);
    }

    /// <summary>Submit a manual payment (uploads the proof image, then records the request).</summary>
    [HttpPost("payment-requests")]
    [ProducesResponseType(typeof(PaymentRequestDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaymentRequestDto>> SubmitPaymentRequest(
        [FromForm] Guid planId,
        [FromForm] Guid? paymentMethodId,
        [FromForm] string? transactionNumber,
        [FromForm] DateTime? paymentDate,
        [FromForm] string? notes,
        IFormFile? proof,
        [FromForm] PaymentRequestOperation operation = PaymentRequestOperation.NewSubscription)
    {
        string? proofUrl = null;
        if (proof != null)
        {
            proofUrl = await _fileUploadService.UploadImageAsync(proof, "payment-proofs");
        }

        var result = await _mediator.Send(new SubmitPaymentRequestCommand
        {
            PlanId = planId,
            PaymentMethodId = paymentMethodId,
            TransactionNumber = transactionNumber,
            PaymentDate = paymentDate,
            ProofFileUrl = proofUrl,
            Notes = notes
            ,Operation = operation
            ,ExtensionDays = Request.Form.TryGetValue("extensionDays", out var days) && int.TryParse(days, out var parsedDays) ? parsedDays : null
        });
        return Ok(result);
    }

    [HttpGet("payment-requests")]
    [ProducesResponseType(typeof(List<PaymentRequestDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<PaymentRequestDto>>> GetMyPaymentRequests()
    {
        var result = await _mediator.Send(new GetMyPaymentRequestsQuery());
        return Ok(result);
    }
}
